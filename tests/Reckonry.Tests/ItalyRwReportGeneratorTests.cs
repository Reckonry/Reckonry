using Reckonry.Core;
using Reckonry.Tax.Italy.Rw;

namespace Reckonry.Tests;

public sealed class ItalyRwReportGeneratorTests
{
    [Fact]
    public void GenerateCryptoDraft_CreatesCryptoRwLineAndRw8Summary()
    {
        var events = new[]
        {
            CreateEvent(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), LedgerEventType.Deposit, "BTC", 1m, LedgerPostingDirection.In)
        };
        var configuration = CompleteConfiguration with
        {
            AllowedForeignTaxCreditsByAsset = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["BTC"] = 1m
            },
            PriorCryptoTaxCredit = 2m,
            CryptoTaxF24Compensations = 0.5m,
            CryptoTaxAdvancesPaid = 3m,
            MonitoringOnly = false
        };

        var report = new ItalyRwReportGenerator().GenerateCryptoDraft(
            2025,
            events,
            configuration,
            new[] { CreateValuation("BTC", initialValue: 90_000m, finalValue: 100_000m) });

        var line = Assert.Single(report.CryptoLines);
        Assert.True(report.CanFinalize);
        Assert.Equal(21, line.Column3AssetCode);
        Assert.Equal(100_000m, line.Column8FinalValue);
        Assert.Equal(365, line.Column10IvafeOrIcHoldingDays);
        Assert.Equal(200m, line.Column33Ic);
        Assert.Equal(199m, line.Column34IcDue);
        Assert.Null(line.Column29Ivafe);
        Assert.Null(line.Column30IvafeDue);
        Assert.Null(line.Column31Ivie);
        Assert.Null(line.Column32IvieDue);

        Assert.Equal(199m, report.Rw8.Column1TotalTaxDue);
        Assert.Equal(194.5m, report.Rw8.Column5TaxDebit);
        Assert.Equal(0m, report.Rw8.Column6TaxCredit);
    }

    [Fact]
    public void GenerateCryptoDraft_BlocksFinalRwWhenOwnershipIsMissing()
    {
        var configuration = CompleteConfiguration with
        {
            OwnershipTitle = null,
            OwnershipPercentage = null
        };

        var report = new ItalyRwReportGenerator().GenerateCryptoDraft(
            2025,
            Array.Empty<LedgerEvent>(),
            configuration,
            new[] { CreateValuation("BTC", 0m, 10m) });

        Assert.False(report.CanFinalize);
        Assert.Contains(report.Errors, error => error.Code == "MissingOwnershipTitle");
        Assert.Contains(report.Errors, error => error.Code == "MissingOwnershipPercentage");
    }

    [Fact]
    public void GenerateCryptoDraft_BlocksFinalRwWhenValuationIsMissing()
    {
        var report = new ItalyRwReportGenerator().GenerateCryptoDraft(
            2025,
            Array.Empty<LedgerEvent>(),
            CompleteConfiguration,
            Array.Empty<ItalyRwAssetValuation>());

        Assert.False(report.CanFinalize);
        var error = Assert.Single(report.Errors, error => error.Code == "MissingValuation");
        Assert.Equal("BTC", error.AssetSymbol);
        var line = Assert.Single(report.CryptoLines);
        Assert.Equal(21, line.Column3AssetCode);
        Assert.Null(line.Column8FinalValue);
        Assert.Null(line.Column33Ic);
    }

    [Fact]
    public void GenerateCryptoDraft_BlocksFinalRwWhenUnknownEventsImpactBalances()
    {
        var events = new[]
        {
            CreateEvent(new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero), LedgerEventType.Unknown, "BTC", 0.1m, LedgerPostingDirection.In)
        };

        var report = new ItalyRwReportGenerator().GenerateCryptoDraft(
            2025,
            events,
            CompleteConfiguration,
            new[] { CreateValuation("BTC", 0m, 10m) });

        Assert.False(report.CanFinalize);
        var error = Assert.Single(report.Errors, error => error.Code == "UnknownEventsImpactBalances");
        Assert.Equal("BTC", error.AssetSymbol);
    }

    [Fact]
    public void GenerateCryptoDraft_WarnsWhenForeignStateIsAmbiguous()
    {
        var configuration = CompleteConfiguration with { ForeignStateCode = null };

        var report = new ItalyRwReportGenerator().GenerateCryptoDraft(
            2025,
            Array.Empty<LedgerEvent>(),
            configuration,
            new[] { CreateValuation("BTC", 0m, 10m) });

        Assert.True(report.CanFinalize);
        Assert.Contains(report.Warnings, warning => warning.Code == "AmbiguousForeignState");
    }

    [Fact]
    public void GenerateCryptoDraft_CountsPartialYearHoldingDays()
    {
        var events = new[]
        {
            CreateEvent(new DateTimeOffset(2025, 7, 1, 12, 0, 0, TimeSpan.Zero), LedgerEventType.Deposit, "BTC", 1m, LedgerPostingDirection.In)
        };

        var report = new ItalyRwReportGenerator().GenerateCryptoDraft(
            2025,
            events,
            CompleteConfiguration,
            new[] { CreateValuation("BTC", 0m, 365_000m) });

        var line = Assert.Single(report.CryptoLines);
        Assert.Equal(184, line.Column10IvafeOrIcHoldingDays);
        Assert.Equal(368m, line.Column33Ic);
    }

    private static ItalyRwReportConfiguration CompleteConfiguration => new()
    {
        OwnershipTitle = RwOwnershipTitle.Property,
        PossessionType = RwPossessionType.BeneficialOwner,
        OwnershipPercentage = 100m,
        PriorCryptoTaxCredit = 0m,
        CryptoTaxF24Compensations = 0m,
        CryptoTaxAdvancesPaid = 0m,
        MonitoringOnly = false,
        ForeignStateCode = "XX",
        CryptoAssetSymbols = new[] { "BTC" }
    };

    private static ItalyRwAssetValuation CreateValuation(
        string assetSymbol,
        decimal initialValue,
        decimal finalValue)
    {
        var timestamp = new DateTimeOffset(2025, 12, 31, 23, 59, 59, TimeSpan.Zero);

        return new ItalyRwAssetValuation(
            assetSymbol,
            RwValuationCriterion.MarketValue,
            new ExchangeValue(initialValue, "Fake Exchange", timestamp, 1m, "Fake test value."),
            new ExchangeValue(finalValue, "Fake Exchange", timestamp, 1m, "Fake test value."));
    }

    private static LedgerEvent CreateEvent(
        DateTimeOffset timestamp,
        LedgerEventType eventType,
        string assetSymbol,
        decimal amount,
        LedgerPostingDirection direction)
    {
        return new LedgerEvent(
            Guid.NewGuid(),
            timestamp,
            eventType,
            $"{eventType} {assetSymbol}",
            new SourceReference("Fake", "fake.csv", 1, "fake,row"),
            new[] { new LedgerPosting(assetSymbol, amount, direction, "Fake:Account") });
    }
}
