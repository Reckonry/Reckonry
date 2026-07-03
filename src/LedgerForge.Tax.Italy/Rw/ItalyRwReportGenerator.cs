using LedgerForge.Core;

namespace LedgerForge.Tax.Italy.Rw;

public sealed class ItalyRwReportGenerator : IItalyRwReportGenerator
{
    private const int CryptoAssetCode = 21;
    private const decimal CryptoTaxRate = 0.002m;

    public ItalyRwReport GenerateCryptoDraft(
        int year,
        IReadOnlyCollection<LedgerEvent> ledgerEvents,
        ItalyRwReportConfiguration configuration,
        IReadOnlyCollection<ItalyRwAssetValuation> valuations)
    {
        ArgumentNullException.ThrowIfNull(ledgerEvents);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(valuations);

        var messages = new List<RwValidationMessage>();
        ValidateConfiguration(configuration, messages);

        var cryptoSymbols = NormalizeSymbols(configuration.CryptoAssetSymbols);
        if (cryptoSymbols.Count == 0)
        {
            messages.Add(new RwValidationMessage(
                RwValidationSeverity.Error,
                "MissingCryptoAssetClassification",
                "Crypto asset symbols must be supplied before RW column 3 can be set to code 21."));
        }

        if (string.IsNullOrWhiteSpace(configuration.ForeignStateCode))
        {
            messages.Add(new RwValidationMessage(
                RwValidationSeverity.Warning,
                "AmbiguousForeignState",
                "RW column 4 is not mandatory for virtual currencies, but foreign-state treatment is ambiguous for crypto custody."));
        }

        var valuationByAsset = valuations
            .Where(valuation => !string.IsNullOrWhiteSpace(valuation.AssetSymbol))
            .GroupBy(valuation => NormalizeSymbol(valuation.AssetSymbol), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var assetSymbol in cryptoSymbols)
        {
            if (!valuationByAsset.ContainsKey(assetSymbol))
            {
                messages.Add(new RwValidationMessage(
                    RwValidationSeverity.Error,
                    "MissingValuation",
                    "Final RW generation is blocked because valuation evidence is missing.",
                    assetSymbol));
            }
        }

        var unknownImpactedAssets = FindUnknownImpactedAssets(ledgerEvents, cryptoSymbols);
        foreach (var assetSymbol in unknownImpactedAssets)
        {
            messages.Add(new RwValidationMessage(
                RwValidationSeverity.Error,
                "UnknownEventsImpactBalances",
                "Final RW generation is blocked because unknown ledger events may affect balances.",
                assetSymbol));
        }

        var lines = BuildLines(year, ledgerEvents, configuration, cryptoSymbols, valuationByAsset);
        var rw8 = BuildRw8(lines, configuration);

        return new ItalyRwReport(year, lines, rw8, messages);
    }

    private static void ValidateConfiguration(
        ItalyRwReportConfiguration configuration,
        List<RwValidationMessage> messages)
    {
        if (configuration.OwnershipTitle is null)
        {
            messages.Add(new RwValidationMessage(
                RwValidationSeverity.Error,
                "MissingOwnershipTitle",
                "Final RW generation is blocked because ownership title is missing."));
        }

        if (configuration.PossessionType is null)
        {
            messages.Add(new RwValidationMessage(
                RwValidationSeverity.Error,
                "MissingPossessionType",
                "Final RW generation is blocked because possession type is missing."));
        }

        if (configuration.OwnershipPercentage is null)
        {
            messages.Add(new RwValidationMessage(
                RwValidationSeverity.Error,
                "MissingOwnershipPercentage",
                "Final RW generation is blocked because ownership percentage is missing."));
        }
        else if (configuration.OwnershipPercentage <= 0m || configuration.OwnershipPercentage > 100m)
        {
            messages.Add(new RwValidationMessage(
                RwValidationSeverity.Error,
                "InvalidOwnershipPercentage",
                "Ownership percentage must be greater than 0 and less than or equal to 100."));
        }

        if (configuration.PriorCryptoTaxCredit is null)
        {
            messages.Add(new RwValidationMessage(
                RwValidationSeverity.Error,
                "MissingPriorCryptoTaxCredit",
                "Final RW generation is blocked because prior crypto tax credit input is missing."));
        }

        if (configuration.CryptoTaxF24Compensations is null)
        {
            messages.Add(new RwValidationMessage(
                RwValidationSeverity.Error,
                "MissingCryptoTaxF24Compensations",
                "Final RW generation is blocked because F24 compensation input is missing."));
        }

        if (configuration.CryptoTaxAdvancesPaid is null)
        {
            messages.Add(new RwValidationMessage(
                RwValidationSeverity.Error,
                "MissingCryptoTaxAdvancesPaid",
                "Final RW generation is blocked because crypto tax advances paid input is missing."));
        }

        if (configuration.MonitoringOnly is null)
        {
            messages.Add(new RwValidationMessage(
                RwValidationSeverity.Error,
                "MissingMonitoringOnlyFlag",
                "Final RW generation is blocked because monitoring-only treatment is missing."));
        }
    }

    private static IReadOnlyList<ItalyRwLine> BuildLines(
        int year,
        IReadOnlyCollection<LedgerEvent> ledgerEvents,
        ItalyRwReportConfiguration configuration,
        IReadOnlyCollection<string> cryptoSymbols,
        IReadOnlyDictionary<string, ItalyRwAssetValuation> valuationByAsset)
    {
        var lineEventsByAsset = ledgerEvents
            .Where(ledgerEvent => ledgerEvent.EventType != LedgerEventType.Unknown)
            .SelectMany(ledgerEvent => ledgerEvent.Postings
                .Where(posting => cryptoSymbols.Contains(NormalizeSymbol(posting.AssetSymbol), StringComparer.OrdinalIgnoreCase))
                .Select(_ => new { AssetSymbol = NormalizeSymbol(_.AssetSymbol), ledgerEvent }))
            .GroupBy(item => item.AssetSymbol, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.ledgerEvent.Id).Distinct().ToArray(),
                StringComparer.OrdinalIgnoreCase);

        var lines = new List<ItalyRwLine>();
        foreach (var assetSymbol in cryptoSymbols.OrderBy(symbol => symbol, StringComparer.OrdinalIgnoreCase))
        {
            valuationByAsset.TryGetValue(assetSymbol, out var valuation);

            var holdingDays = CountHoldingDays(year, assetSymbol, ledgerEvents);
            var foreignTaxCredit = GetAllowedForeignTaxCredit(configuration, assetSymbol);
            var ic = valuation is null
                ? (decimal?)null
                : CalculateCryptoTax(valuation.FinalValue.ValueEur, configuration.OwnershipPercentage, holdingDays, year);
            var icDue = ic is null ? (decimal?)null : Math.Max(0m, ic.Value - foreignTaxCredit);
            var coOwners = configuration.CoOwners.ToArray();

            lines.Add(new ItalyRwLine
            {
                AssetSymbol = assetSymbol,
                SourceLedgerEventIds = lineEventsByAsset.TryGetValue(assetSymbol, out var eventIds)
                    ? eventIds
                    : Array.Empty<Guid>(),
                InitialValueEvidence = valuation?.InitialValue,
                FinalValueEvidence = valuation?.FinalValue,
                Column1OwnershipTitle = configuration.OwnershipTitle,
                Column2PossessionType = configuration.PossessionType,
                Column3AssetCode = CryptoAssetCode,
                Column4ForeignStateCode = string.IsNullOrWhiteSpace(configuration.ForeignStateCode)
                    ? null
                    : configuration.ForeignStateCode.Trim(),
                Column5OwnershipPercentage = configuration.OwnershipPercentage,
                Column6ValuationCriterion = valuation?.ValuationCriterion,
                Column7InitialValue = valuation?.InitialValue.ValueEur,
                Column8FinalValue = valuation?.FinalValue.ValueEur,
                Column10IvafeOrIcHoldingDays = holdingDays,
                Column12ForeignTaxCredit = foreignTaxCredit,
                Column16MonitoringOnly = configuration.MonitoringOnly == true,
                Column18CoOwnerTaxCode = coOwners.Length > 0 ? coOwners[0] : null,
                Column19CoOwnerTaxCode = coOwners.Length > 1 ? coOwners[1] : null,
                Column20MoreThanTwoCoOwners = coOwners.Length > 2,
                Column33Ic = configuration.MonitoringOnly == true ? null : ic,
                Column34IcDue = configuration.MonitoringOnly == true ? null : icDue
            });
        }

        return lines;
    }

    private static ItalyRw8Summary BuildRw8(
        IReadOnlyCollection<ItalyRwLine> lines,
        ItalyRwReportConfiguration configuration)
    {
        var totalTaxDue = lines.Sum(line => line.Column34IcDue ?? 0m);
        var balance = totalTaxDue
            - (configuration.PriorCryptoTaxCredit ?? 0m)
            + (configuration.CryptoTaxF24Compensations ?? 0m)
            - (configuration.CryptoTaxAdvancesPaid ?? 0m);

        return new ItalyRw8Summary
        {
            Column1TotalTaxDue = totalTaxDue,
            Column2PreviousDeclarationExcess = configuration.PriorCryptoTaxCredit ?? 0m,
            Column3F24CompensatedExcess = configuration.CryptoTaxF24Compensations ?? 0m,
            Column4AdvancesPaid = configuration.CryptoTaxAdvancesPaid ?? 0m,
            Column5TaxDebit = balance > 0m ? balance : 0m,
            Column6TaxCredit = balance < 0m ? Math.Abs(balance) : 0m
        };
    }

    private static decimal CalculateCryptoTax(
        decimal finalValue,
        decimal? ownershipPercentage,
        int holdingDays,
        int year)
    {
        if (ownershipPercentage is null || ownershipPercentage <= 0m || holdingDays <= 0)
        {
            return 0m;
        }

        var ownershipQuota = ownershipPercentage.Value / 100m;
        var yearDays = DateTime.IsLeapYear(year) ? 366m : 365m;

        return finalValue * ownershipQuota * holdingDays / yearDays * CryptoTaxRate;
    }

    private static int CountHoldingDays(
        int year,
        string assetSymbol,
        IReadOnlyCollection<LedgerEvent> ledgerEvents)
    {
        var normalizedAsset = NormalizeSymbol(assetSymbol);
        var start = new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endExclusive = start.AddYears(1);
        var eventsByDay = ledgerEvents
            .Where(ledgerEvent => ledgerEvent.EventType != LedgerEventType.Unknown)
            .Where(ledgerEvent => ledgerEvent.TimestampUtc < endExclusive)
            .SelectMany(ledgerEvent => ledgerEvent.Postings
                .Where(posting => string.Equals(NormalizeSymbol(posting.AssetSymbol), normalizedAsset, StringComparison.OrdinalIgnoreCase))
                .Select(posting => new PostingMovement(ledgerEvent.TimestampUtc, ToSignedAmount(posting))))
            .OrderBy(movement => movement.Timestamp)
            .ToArray();

        var balance = eventsByDay
            .Where(movement => movement.Timestamp < start)
            .Sum(movement => movement.SignedAmount);

        var movementsInYear = eventsByDay
            .Where(movement => movement.Timestamp >= start)
            .GroupBy(movement => DateOnly.FromDateTime(movement.Timestamp.UtcDateTime.Date))
            .ToDictionary(group => group.Key, group => group.ToArray());

        var holdingDays = 0;
        for (var day = DateOnly.FromDateTime(start.UtcDateTime.Date);
             day.Year == year;
             day = day.AddDays(1))
        {
            var heldDuringDay = balance > 0m;
            if (movementsInYear.TryGetValue(day, out var movements))
            {
                foreach (var movement in movements.OrderBy(movement => movement.Timestamp))
                {
                    balance += movement.SignedAmount;
                    if (balance > 0m)
                    {
                        heldDuringDay = true;
                    }
                }
            }

            if (heldDuringDay)
            {
                holdingDays++;
            }
        }

        return holdingDays;
    }

    private static decimal ToSignedAmount(LedgerPosting posting)
    {
        return posting.Direction == LedgerPostingDirection.In ? posting.Amount : -posting.Amount;
    }

    private static IReadOnlyCollection<string> FindUnknownImpactedAssets(
        IReadOnlyCollection<LedgerEvent> ledgerEvents,
        IReadOnlyCollection<string> cryptoSymbols)
    {
        var impactedAssets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var ledgerEvent in ledgerEvents.Where(ledgerEvent => ledgerEvent.EventType == LedgerEventType.Unknown))
        {
            var unknownAssetSymbols = ledgerEvent.Postings
                .Select(posting => NormalizeSymbol(posting.AssetSymbol))
                .Where(asset => !string.IsNullOrWhiteSpace(asset))
                .ToArray();

            if (unknownAssetSymbols.Length == 0)
            {
                foreach (var assetSymbol in cryptoSymbols)
                {
                    impactedAssets.Add(assetSymbol);
                }

                continue;
            }

            foreach (var assetSymbol in unknownAssetSymbols.Where(asset => cryptoSymbols.Contains(asset, StringComparer.OrdinalIgnoreCase)))
            {
                impactedAssets.Add(assetSymbol);
            }
        }

        return impactedAssets;
    }

    private static decimal GetAllowedForeignTaxCredit(
        ItalyRwReportConfiguration configuration,
        string assetSymbol)
    {
        foreach (var creditByAsset in configuration.AllowedForeignTaxCreditsByAsset)
        {
            if (string.Equals(NormalizeSymbol(creditByAsset.Key), assetSymbol, StringComparison.OrdinalIgnoreCase))
            {
                return Math.Max(0m, creditByAsset.Value);
            }
        }

        return 0m;
    }

    private static IReadOnlyCollection<string> NormalizeSymbols(IEnumerable<string> symbols)
    {
        return symbols
            .Select(NormalizeSymbol)
            .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string NormalizeSymbol(string? symbol)
    {
        return string.IsNullOrWhiteSpace(symbol) ? string.Empty : symbol.Trim().ToUpperInvariant();
    }

    private sealed record PostingMovement(DateTimeOffset Timestamp, decimal SignedAmount);
}
