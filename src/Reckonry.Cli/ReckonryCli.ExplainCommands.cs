using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Reckonry.Audit;
using Reckonry.Core;
using Reckonry.Importers.Abstractions;
using Reckonry.Tax.Italy.Rw;

internal static partial class ReckonryCli
{
    private static readonly JsonSerializerOptions ExplainJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static async Task<int> ExplainAsync(string[] args, AppServices services)
    {
        var input = GetOption(args, "--input") ?? GetFirstPositional(args);
        var ledgerPath = GetOption(args, "--ledger");
        var output = GetOption(args, "--out");
        var yearText = GetOption(args, "--year");

        if (string.IsNullOrWhiteSpace(input))
        {
            WriteError(
                "Missing required input artifact.",
                "reckonry explain --input <artifact> [--ledger <ledger.json>] [--year <year>] [--out <explanation.md>]");
            return ExitUsage;
        }

        WriteInputSafetyWarning(input);
        if (!string.IsNullOrWhiteSpace(ledgerPath))
        {
            WriteInputSafetyWarning(ledgerPath);
        }

        var resolvedInput = ResolveStructuredArtifact(input);
        if (resolvedInput is null)
        {
            WriteError(
                $"Cannot explain `{input}` as structured data.",
                hint: "Use the generated JSON or CSV companion artifact. PDF explanation is not supported because the structure is not machine-verifiable.");
            return ExitDataError;
        }

        if (!File.Exists(resolvedInput))
        {
            WriteError($"Input artifact was not found: {resolvedInput}");
            return ExitNoInput;
        }

        WritePhase($"Explaining {Path.GetFileName(resolvedInput)}");

        string explanation;
        try
        {
            explanation = await BuildExplanationAsync(resolvedInput, ledgerPath, yearText, services);
        }
        catch (FileNotFoundException ex)
        {
            WriteError(ex.Message);
            return ExitNoInput;
        }
        catch (InvalidDataException ex)
        {
            WriteError(ex.Message);
            return ExitDataError;
        }
        catch (ArgumentException ex)
        {
            WriteError(ex.Message);
            return ExitUsage;
        }

        if (string.IsNullOrWhiteSpace(output))
        {
            Console.WriteLine(explanation.TrimEnd());
        }
        else
        {
            var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(output));
            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            await File.WriteAllTextAsync(output, explanation);
            WriteSuccess("Explanation generated.");
            WriteInfo("File", output);
        }

        WriteNext("reckonry doctor demo");
        return ExitSuccess;
    }

    private static async Task<string> BuildExplanationAsync(
        string input,
        string? ledgerPath,
        string? yearText,
        AppServices services)
    {
        if (await LooksLikeLedgerAsync(input))
        {
            var ledgerEvents = await services.LedgerStore.ReadAsync(input);
            return ExplainLedger(input, ledgerEvents);
        }

        if (string.IsNullOrWhiteSpace(ledgerPath))
        {
            throw new ArgumentException("Explaining generated reports requires `--ledger <ledger.json>` so every number can trace back to source rows, ledger events, and postings.");
        }

        if (!File.Exists(ledgerPath))
        {
            throw new FileNotFoundException($"Ledger file was not found: {ledgerPath}");
        }

        var events = await services.LedgerStore.ReadAsync(ledgerPath);
        var fileName = Path.GetFileName(input);
        if (fileName.StartsWith("rw-snapshot-", StringComparison.OrdinalIgnoreCase))
        {
            var year = ResolveYear(input, yearText);
            return ExplainRwSnapshot(input, ledgerPath, year, events);
        }

        if (fileName.StartsWith("rw-value-", StringComparison.OrdinalIgnoreCase))
        {
            var year = ResolveYear(input, yearText);
            return ExplainRwValue(input, ledgerPath, year, events);
        }

        if (fileName.Equals("integrity.json", StringComparison.OrdinalIgnoreCase))
        {
            var report = await ReadJsonAsync<LedgerIntegrityReport>(input);
            return ExplainIntegrityReport(input, ledgerPath, events, report);
        }

        throw new InvalidDataException($"Unsupported structured artifact for explanation: {input}");
    }

    private static string ExplainLedger(string ledgerPath, IReadOnlyList<LedgerEvent> events)
    {
        var builder = CreateExplanationHeader("Canonical ledger", ledgerPath, ledgerPath);
        builder.AppendLine("This explanation enumerates the canonical facts available in the ledger. It does not derive tax, accounting, or valuation conclusions.");
        builder.AppendLine();
        builder.AppendLine("## Reported numbers");
        builder.AppendLine($"- Total ledger events: `{events.Count}`. Report: `{ledgerPath}`. Source: counted from `events[]`.");
        builder.AppendLine($"- Total postings: `{events.Sum(e => e.Postings.Count)}`. Report: `{ledgerPath}`. Source: sum of `event.postings[]` counts.");
        builder.AppendLine();

        foreach (var ledgerEvent in events.OrderBy(e => e.TimestampUtc))
        {
            AppendEvent(builder, ledgerEvent);
        }

        return builder.ToString();
    }

    private static string ExplainRwSnapshot(
        string reportPath,
        string ledgerPath,
        int year,
        IReadOnlyList<LedgerEvent> events)
    {
        var rows = ReadRwSnapshotRows(reportPath);
        var traces = BuildRwSnapshotTraces(year, events);
        var builder = CreateExplanationHeader("Italy RW snapshot report", reportPath, ledgerPath);
        builder.AppendLine($"Year: `{year}`");
        builder.AppendLine();

        foreach (var row in rows)
        {
            var trace = traces.GetValueOrDefault(row.AssetSymbol, RwSnapshotTrace.Empty(row.AssetSymbol));
            builder.AppendLine($"## Report row: `{row.AssetSymbol}`");
            builder.AppendLine($"Report: `{reportPath}`");
            AppendNumber(builder, "OpeningQuantity", row.OpeningQuantity, "sum of signed candidate-crypto postings before the report year", trace.Opening);
            AppendNumber(builder, "ClosingQuantity", row.ClosingQuantity, "opening quantity plus signed candidate-crypto postings during the report year", trace.Closing);
            AppendNumber(builder, "IncomingQuantity", row.IncomingQuantity, "sum of incoming candidate-crypto postings during the report year", trace.Incoming);
            AppendNumber(builder, "OutgoingQuantity", row.OutgoingQuantity, "sum of outgoing candidate-crypto postings during the report year", trace.Outgoing);
            AppendUnknownCount(builder, row.UnknownEventCount, trace.UnknownEvents);
            AppendWarning(builder, row.Warning, trace.UnknownEvents);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string ExplainRwValue(
        string reportPath,
        string ledgerPath,
        int year,
        IReadOnlyList<LedgerEvent> events)
    {
        var rows = ReadRwValueRows(reportPath);
        var traces = BuildRwValueTraces(year, events);
        var builder = CreateExplanationHeader("Italy RW value report", reportPath, ledgerPath);
        builder.AppendLine($"Year: `{year}`");
        builder.AppendLine();

        foreach (var row in rows)
        {
            var trace = traces.GetValueOrDefault(row.AssetSymbol, RwValueTrace.Empty(row.AssetSymbol));
            builder.AppendLine($"## Report row: `{row.AssetSymbol}`");
            builder.AppendLine($"Report: `{reportPath}`");
            AppendNumber(builder, "OpeningQuantity", row.OpeningQuantity, "sum of signed candidate-crypto postings before the report year", trace.Opening);
            AppendNumber(builder, "ClosingQuantity", row.ClosingQuantity, "opening quantity plus signed candidate-crypto postings during the report year", trace.Closing);
            AppendNumber(builder, "IncomingValueEUR", row.IncomingValueEUR, "sum of EUR values on incoming non-fee postings during the report year", trace.IncomingValue);
            AppendNumber(builder, "OutgoingValueEUR", row.OutgoingValueEUR, "sum of EUR values on outgoing non-fee postings during the report year", trace.OutgoingValue);
            AppendNumber(builder, "FeeValueEUR", row.FeeValueEUR, "sum of EUR values on fee postings during the report year", trace.FeeValue);
            AppendWarning(builder, row.Warning, trace.UnknownEvents.Concat(trace.MissingValuePostings).ToArray());
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string ExplainIntegrityReport(
        string reportPath,
        string ledgerPath,
        IReadOnlyList<LedgerEvent> events,
        LedgerIntegrityReport report)
    {
        var builder = CreateExplanationHeader("Ledger integrity report", reportPath, ledgerPath);
        builder.AppendLine("## Reported numbers");
        AppendScalar(builder, "TotalEvents", report.TotalEvents, "count of ledger events", events.Select(EventOnlyContribution).ToArray(), reportPath);
        AppendScalar(builder, "TotalPostings", report.TotalPostings, "sum of postings across all ledger events", events.SelectMany(EventPostingContributions).ToArray(), reportPath);

        var penaltyContributions = report.Findings
            .Select(f => new TextContribution($"{f.Code}: {f.Severity} => {PenaltyFor(f.Severity)} point penalty; finding count {f.Count}. Report: `{reportPath}`"))
            .ToArray();
        AppendScalar(builder, "IntegrityScore", report.IntegrityScore, "100 minus severity penalties, clamped from 0 to 100", penaltyContributions, reportPath);

        var unknownCount = events.Count(e => e.EventType == LedgerEventType.Unknown);
        var confidence = penaltyContributions
            .Concat([
                new TextContribution($"Unknown event count `{unknownCount}` over total events `{events.Count}` contributes the unknown-event confidence adjustment. Report: `{reportPath}`")
            ])
            .ToArray();
        AppendScalar(builder, "ConfidenceScore", report.ConfidenceScore, "integrity score adjusted for empty ledgers, empty postings, and unknown-event ratio", confidence, reportPath);

        builder.AppendLine();
        builder.AppendLine("## Findings");
        foreach (var finding in report.Findings)
        {
            builder.AppendLine($"### `{finding.Code}`");
            builder.AppendLine($"- Reported count: `{finding.Count}`. Report: `{reportPath}`.");
            builder.AppendLine($"- Severity: `{finding.Severity}`.");
            builder.AppendLine($"- Message: {finding.Message}");
            AppendFindingEvidence(builder, finding.Code, events);
            builder.AppendLine();
        }

        if (report.Findings.Count == 0)
        {
            builder.AppendLine("No findings were reported.");
        }

        return builder.ToString();
    }

    private static StringBuilder CreateExplanationHeader(string title, string reportPath, string ledgerPath)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# Reckonry Explain: {title}");
        builder.AppendLine();
        builder.AppendLine("This explanation is derived only from the supplied artifact and canonical ledger. It does not invent missing data.");
        builder.AppendLine($"- Report: `{reportPath}`");
        builder.AppendLine($"- Ledger: `{ledgerPath}`");
        builder.AppendLine();
        return builder;
    }

    private static void AppendNumber(
        StringBuilder builder,
        string name,
        decimal reported,
        string formula,
        IReadOnlyCollection<NumberContribution> contributions)
    {
        builder.AppendLine($"### `{name}` = `{FormatDecimal(reported)}`");
        builder.AppendLine($"- Formula: {formula}.");
        if (contributions.Count == 0)
        {
            builder.AppendLine("- Contributions: none. The reported value is zero because no matching ledger postings were available.");
            return;
        }

        builder.AppendLine("- Contributions:");
        foreach (var contribution in contributions)
        {
            builder.AppendLine($"  - {FormatDecimal(contribution.Amount)} from {DescribeContribution(contribution)}");
        }
    }

    private static void AppendUnknownCount(
        StringBuilder builder,
        int reported,
        IReadOnlyCollection<LedgerEvent> unknownEvents)
    {
        builder.AppendLine($"### `UnknownEventCount` = `{reported}`");
        builder.AppendLine("- Formula: count unknown ledger events affecting this report row.");
        if (unknownEvents.Count == 0)
        {
            builder.AppendLine("- Contributions: none.");
            return;
        }

        builder.AppendLine("- Contributions:");
        foreach (var ledgerEvent in unknownEvents)
        {
            builder.AppendLine($"  - 1 from {DescribeEvent(ledgerEvent)}");
        }
    }

    private static void AppendWarning(StringBuilder builder, string warning, IReadOnlyCollection<LedgerEvent> events)
    {
        builder.AppendLine($"### `Warning` = `{(string.IsNullOrWhiteSpace(warning) ? "<empty>" : warning)}`");
        if (string.IsNullOrWhiteSpace(warning))
        {
            builder.AppendLine("- Explanation: no warning text was reported for this row.");
            return;
        }

        builder.AppendLine("- Explanation: warning is supported by the following ledger evidence:");
        if (events.Count == 0)
        {
            builder.AppendLine("  - No direct ledger event could be identified from the artifact. Review the report generator logic.");
            return;
        }

        foreach (var ledgerEvent in events)
        {
            builder.AppendLine($"  - {DescribeEvent(ledgerEvent)}");
        }
    }

    private static void AppendScalar(
        StringBuilder builder,
        string name,
        int reported,
        string formula,
        IReadOnlyCollection<TextContribution> contributions,
        string reportPath)
    {
        builder.AppendLine($"### `{name}` = `{reported}`");
        builder.AppendLine($"- Formula: {formula}.");
        builder.AppendLine($"- Report: `{reportPath}`.");
        if (contributions.Count == 0)
        {
            builder.AppendLine("- Contributions: none.");
            return;
        }

        builder.AppendLine("- Contributions:");
        foreach (var contribution in contributions)
        {
            builder.AppendLine($"  - {contribution.Text}");
        }
    }

    private static void AppendEvent(StringBuilder builder, LedgerEvent ledgerEvent)
    {
        builder.AppendLine($"## Ledger event `{ledgerEvent.Id}`");
        builder.AppendLine($"- Timestamp: `{ledgerEvent.TimestampUtc:O}`");
        builder.AppendLine($"- Type: `{ledgerEvent.EventType}`");
        builder.AppendLine($"- Description: {ledgerEvent.Description}");
        builder.AppendLine($"- Source row: {DescribeSource(ledgerEvent.SourceReference)}");
        builder.AppendLine($"- Posting count: `{ledgerEvent.Postings.Count}`");

        for (var i = 0; i < ledgerEvent.Postings.Count; i++)
        {
            var posting = ledgerEvent.Postings[i];
            builder.AppendLine($"  - Posting {i + 1}: `{posting.Direction}` `{FormatDecimal(posting.Amount)}` `{posting.AssetSymbol}` account `{posting.Account}`.");
            if (posting.Value is not null)
            {
                builder.AppendLine($"    Value: `{FormatDecimal(posting.Value.Amount)}` `{posting.Value.CurrencyCode}`.");
            }
        }

        builder.AppendLine();
    }

    private static void AppendFindingEvidence(StringBuilder builder, string code, IReadOnlyList<LedgerEvent> events)
    {
        var evidence = code switch
        {
            "NEGATIVE_BALANCES" => NegativeBalanceEvents(events),
            "UNKNOWN_EVENT_RATIO" => events.Where(e => e.EventType == LedgerEventType.Unknown).ToArray(),
            "UNKNOWN_POSTING_RATIO" => events.Where(e => e.EventType == LedgerEventType.Unknown && e.Postings.Count > 0).ToArray(),
            "TIMESTAMP_ANOMALIES" => events.Where(e => e.TimestampUtc == default || e.TimestampUtc == DateTimeOffset.UnixEpoch || e.TimestampUtc.Offset != TimeSpan.Zero || e.TimestampUtc.Year is < 2009 or > 2100).ToArray(),
            "MISSING_SOURCE_REFERENCES" => events.Where(e => e.SourceReference is null || string.IsNullOrWhiteSpace(e.SourceReference.SourceSystem) || string.IsNullOrWhiteSpace(e.SourceReference.SourceFile) || e.SourceReference.SourceRowNumber <= 0 || string.IsNullOrWhiteSpace(e.SourceReference.RawData)).ToArray(),
            "BROKEN_TRANSFERS" => events.Where(e => e.EventType == LedgerEventType.Transfer && (!e.Postings.Any(p => p.Direction == LedgerPostingDirection.In) || !e.Postings.Any(p => p.Direction == LedgerPostingDirection.Out))).ToArray(),
            "MISSING_ASSETS" => events.Where(e => e.Postings.Any(p => string.IsNullOrWhiteSpace(p.AssetSymbol))).ToArray(),
            "FEE_ANOMALIES" => events.Where(e => e.Postings.Any(p => p.Account.Contains("fee", StringComparison.OrdinalIgnoreCase) && (p.Direction != LedgerPostingDirection.Out || p.Amount <= 0))).ToArray(),
            "CURRENCY_ANOMALIES" => events.Where(e => e.Postings.Any(p => p.Value is not null && (string.IsNullOrWhiteSpace(p.Value.CurrencyCode) || p.Value.CurrencyCode.Length != 3 || p.Value.Amount < 0))).ToArray(),
            _ => []
        };

        if (evidence.Count == 0)
        {
            builder.AppendLine("- Evidence: this finding does not expose row-level evidence in the report artifact.");
            return;
        }

        builder.AppendLine("- Evidence:");
        foreach (var ledgerEvent in evidence)
        {
            builder.AppendLine($"  - {DescribeEvent(ledgerEvent)}");
        }
    }

    private static IReadOnlyList<LedgerEvent> NegativeBalanceEvents(IReadOnlyList<LedgerEvent> events)
    {
        var balances = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var evidence = new List<LedgerEvent>();

        foreach (var ledgerEvent in events.OrderBy(e => e.TimestampUtc))
        {
            var eventWentNegative = false;
            foreach (var posting in ledgerEvent.Postings.Where(p => !string.IsNullOrWhiteSpace(p.AssetSymbol)))
            {
                var asset = posting.AssetSymbol.Trim().ToUpperInvariant();
                var signed = posting.Direction == LedgerPostingDirection.In ? posting.Amount : -posting.Amount;
                balances[asset] = balances.GetValueOrDefault(asset) + signed;
                eventWentNegative |= balances[asset] < 0;
            }

            if (eventWentNegative)
            {
                evidence.Add(ledgerEvent);
            }
        }

        return evidence;
    }

    private static Dictionary<string, RwSnapshotTrace> BuildRwSnapshotTraces(int year, IReadOnlyList<LedgerEvent> events)
    {
        var start = new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endExclusive = start.AddYears(1);
        var traces = new Dictionary<string, RwSnapshotTrace>(StringComparer.OrdinalIgnoreCase);

        foreach (var ledgerEvent in events.OrderBy(e => e.TimestampUtc))
        {
            if (ledgerEvent.EventType == LedgerEventType.Unknown)
            {
                AddUnknownTrace(traces, ledgerEvent);
                continue;
            }

            for (var i = 0; i < ledgerEvent.Postings.Count; i++)
            {
                var posting = ledgerEvent.Postings[i];
                if (!IsCandidateCryptoAsset(posting.AssetSymbol))
                {
                    continue;
                }

                var trace = GetSnapshotTrace(traces, posting.AssetSymbol);
                var signed = posting.Direction == LedgerPostingDirection.In ? posting.Amount : -posting.Amount;
                var contribution = new NumberContribution(signed, ledgerEvent, i, posting);

                if (ledgerEvent.TimestampUtc < start)
                {
                    trace.Opening.Add(contribution);
                    trace.Closing.Add(contribution);
                    continue;
                }

                if (ledgerEvent.TimestampUtc >= start && ledgerEvent.TimestampUtc < endExclusive)
                {
                    trace.Closing.Add(contribution);
                    if (posting.Direction == LedgerPostingDirection.In)
                    {
                        trace.Incoming.Add(new NumberContribution(posting.Amount, ledgerEvent, i, posting));
                    }
                    else
                    {
                        trace.Outgoing.Add(new NumberContribution(posting.Amount, ledgerEvent, i, posting));
                    }
                }
            }
        }

        return traces;
    }

    private static Dictionary<string, RwValueTrace> BuildRwValueTraces(int year, IReadOnlyList<LedgerEvent> events)
    {
        var start = new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endExclusive = start.AddYears(1);
        var traces = new Dictionary<string, RwValueTrace>(StringComparer.OrdinalIgnoreCase);

        foreach (var ledgerEvent in events.OrderBy(e => e.TimestampUtc))
        {
            if (ledgerEvent.EventType == LedgerEventType.Unknown)
            {
                AddUnknownTrace(traces, ledgerEvent);
                continue;
            }

            for (var i = 0; i < ledgerEvent.Postings.Count; i++)
            {
                var posting = ledgerEvent.Postings[i];
                if (!IsCandidateCryptoAsset(posting.AssetSymbol))
                {
                    continue;
                }

                var trace = GetValueTrace(traces, posting.AssetSymbol);
                var signed = posting.Direction == LedgerPostingDirection.In ? posting.Amount : -posting.Amount;
                var quantityContribution = new NumberContribution(signed, ledgerEvent, i, posting);

                if (ledgerEvent.TimestampUtc < start)
                {
                    trace.Opening.Add(quantityContribution);
                    trace.Closing.Add(quantityContribution);
                    continue;
                }

                if (ledgerEvent.TimestampUtc < endExclusive)
                {
                    trace.Closing.Add(quantityContribution);
                    if (posting.Value is null || !string.Equals(posting.Value.CurrencyCode, "EUR", StringComparison.OrdinalIgnoreCase))
                    {
                        trace.MissingValuePostings.Add(ledgerEvent);
                        continue;
                    }

                    var valueContribution = new NumberContribution(posting.Value.Amount, ledgerEvent, i, posting);
                    if (string.Equals(posting.Account, "Binance:Fees", StringComparison.OrdinalIgnoreCase))
                    {
                        trace.FeeValue.Add(valueContribution);
                    }
                    else if (posting.Direction == LedgerPostingDirection.In)
                    {
                        trace.IncomingValue.Add(valueContribution);
                    }
                    else
                    {
                        trace.OutgoingValue.Add(valueContribution);
                    }
                }
            }
        }

        return traces;
    }

    private static void AddUnknownTrace<TTrace>(Dictionary<string, TTrace> traces, LedgerEvent ledgerEvent)
        where TTrace : IUnknownTrace
    {
        var assetSymbols = ledgerEvent.Postings
            .Select(p => p.AssetSymbol)
            .Where(asset => !string.IsNullOrWhiteSpace(asset))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (assetSymbols.Length == 0)
        {
            GetTrace(traces, "UNKNOWN").UnknownEvents.Add(ledgerEvent);
            return;
        }

        foreach (var assetSymbol in assetSymbols)
        {
            GetTrace(traces, assetSymbol).UnknownEvents.Add(ledgerEvent);
        }
    }

    private static TTrace GetTrace<TTrace>(Dictionary<string, TTrace> traces, string assetSymbol)
        where TTrace : IUnknownTrace
    {
        return typeof(TTrace) == typeof(RwSnapshotTrace)
            ? (TTrace)(object)GetSnapshotTrace((Dictionary<string, RwSnapshotTrace>)(object)traces, assetSymbol)
            : (TTrace)(object)GetValueTrace((Dictionary<string, RwValueTrace>)(object)traces, assetSymbol);
    }

    private static RwSnapshotTrace GetSnapshotTrace(Dictionary<string, RwSnapshotTrace> traces, string assetSymbol)
    {
        var normalized = NormalizeAsset(assetSymbol);
        if (!traces.TryGetValue(normalized, out var trace))
        {
            trace = RwSnapshotTrace.Empty(normalized);
            traces[normalized] = trace;
        }

        return trace;
    }

    private static RwValueTrace GetValueTrace(Dictionary<string, RwValueTrace> traces, string assetSymbol)
    {
        var normalized = NormalizeAsset(assetSymbol);
        if (!traces.TryGetValue(normalized, out var trace))
        {
            trace = RwValueTrace.Empty(normalized);
            traces[normalized] = trace;
        }

        return trace;
    }

    private static IReadOnlyList<RwSnapshotRow> ReadRwSnapshotRows(string path)
    {
        if (Path.GetExtension(path).Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return ReadCsv(path)
                .Select(row => new RwSnapshotRow(
                    row["AssetSymbol"],
                    ParseDecimal(row["OpeningQuantity"]),
                    ParseDecimal(row["ClosingQuantity"]),
                    ParseDecimal(row["IncomingQuantity"]),
                    ParseDecimal(row["OutgoingQuantity"]),
                    int.Parse(row["UnknownEventCount"], CultureInfo.InvariantCulture),
                    row.GetValueOrDefault("Warning", string.Empty)))
                .ToArray();
        }

        return ReadJsonAsync<IReadOnlyList<RwSnapshotRow>>(path).GetAwaiter().GetResult();
    }

    private static IReadOnlyList<RwValueRow> ReadRwValueRows(string path)
    {
        if (Path.GetExtension(path).Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return ReadCsv(path)
                .Select(row => new RwValueRow(
                    row["AssetSymbol"],
                    ParseDecimal(row["OpeningQuantity"]),
                    ParseDecimal(row["ClosingQuantity"]),
                    ParseDecimal(row["IncomingValueEUR"]),
                    ParseDecimal(row["OutgoingValueEUR"]),
                    ParseDecimal(row["FeeValueEUR"]),
                    row.GetValueOrDefault("Warning", string.Empty)))
                .ToArray();
        }

        return ReadJsonAsync<IReadOnlyList<RwValueRow>>(path).GetAwaiter().GetResult();
    }

    private static async Task<T> ReadJsonAsync<T>(string path)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, ExplainJsonOptions)
            ?? throw new InvalidDataException($"Could not deserialize structured artifact: {path}");
    }

    private static async Task<bool> LooksLikeLedgerAsync(string path)
    {
        if (!Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        await using var stream = File.OpenRead(path);
        using var document = await JsonDocument.ParseAsync(stream);
        if (document.RootElement.ValueKind == JsonValueKind.Object)
        {
            return document.RootElement.TryGetProperty("events", out _);
        }

        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var firstElement = document.RootElement.EnumerateArray().FirstOrDefault();
        return firstElement.ValueKind == JsonValueKind.Object
            && firstElement.TryGetProperty("eventType", out _)
            && firstElement.TryGetProperty("sourceReference", out _)
            && firstElement.TryGetProperty("postings", out _);
    }

    private static string? ResolveStructuredArtifact(string input)
    {
        if (Path.GetExtension(input).Equals(".md", StringComparison.OrdinalIgnoreCase))
        {
            var json = Path.ChangeExtension(input, ".json");
            return File.Exists(json) ? json : null;
        }

        if (Path.GetExtension(input).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return input;
    }

    private static int ResolveYear(string path, string? yearText)
    {
        if (!string.IsNullOrWhiteSpace(yearText))
        {
            if (int.TryParse(yearText, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedYear) && parsedYear is >= 1 and <= 9999)
            {
                return parsedYear;
            }

            throw new ArgumentException($"Invalid year: {yearText}");
        }

        var fileName = Path.GetFileNameWithoutExtension(path);
        var token = fileName.Split('-', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out var inferredYear))
        {
            return inferredYear;
        }

        throw new ArgumentException("Could not infer report year from artifact name. Pass `--year <year>`.");
    }

    private static string? GetFirstPositional(IReadOnlyList<string> args)
    {
        for (var i = 0; i < args.Count; i++)
        {
            if (args[i].StartsWith("--", StringComparison.Ordinal))
            {
                i++;
                continue;
            }

            return args[i];
        }

        return null;
    }

    private static IReadOnlyList<Dictionary<string, string>> ReadCsv(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0)
        {
            return [];
        }

        var headers = SplitCsvLine(lines[0]);
        return lines
            .Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line =>
            {
                var values = SplitCsvLine(line);
                return headers.Select((header, index) => new { header, value = index < values.Count ? values[index] : string.Empty })
                    .ToDictionary(item => item.header, item => item.value, StringComparer.OrdinalIgnoreCase);
            })
            .ToArray();
    }

    private static IReadOnlyList<string> SplitCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var character = line[i];
            if (character == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (character == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(character);
            }
        }

        values.Add(current.ToString());
        return values;
    }

    private static TextContribution EventOnlyContribution(LedgerEvent ledgerEvent)
    {
        return new TextContribution($"1 event from {DescribeEvent(ledgerEvent)}");
    }

    private static IEnumerable<TextContribution> EventPostingContributions(LedgerEvent ledgerEvent)
    {
        for (var i = 0; i < ledgerEvent.Postings.Count; i++)
        {
            yield return new TextContribution($"1 posting from {DescribeContribution(new NumberContribution(ledgerEvent.Postings[i].Amount, ledgerEvent, i, ledgerEvent.Postings[i]))}");
        }
    }

    private static string DescribeContribution(NumberContribution contribution)
    {
        return $"ledger event `{contribution.Event.Id}`, posting `{contribution.PostingIndex + 1}` (`{contribution.Posting.Direction}` `{FormatDecimal(contribution.Posting.Amount)}` `{contribution.Posting.AssetSymbol}` account `{contribution.Posting.Account}`), source row {DescribeSource(contribution.Event.SourceReference)}";
    }

    private static string DescribeEvent(LedgerEvent ledgerEvent)
    {
        return $"ledger event `{ledgerEvent.Id}` (`{ledgerEvent.EventType}` at `{ledgerEvent.TimestampUtc:O}`), source row {DescribeSource(ledgerEvent.SourceReference)}";
    }

    private static string DescribeSource(SourceReference source)
    {
        return $"`{source.SourceSystem}` `{source.SourceFile}:{source.SourceRowNumber}` raw `{source.RawData}`";
    }

    private static int PenaltyFor(IntegritySeverity severity)
    {
        return severity switch
        {
            IntegritySeverity.Error => 15,
            IntegritySeverity.Warning => 7,
            _ => 2
        };
    }

    private static bool IsCandidateCryptoAsset(string assetSymbol)
    {
        return !string.IsNullOrWhiteSpace(assetSymbol)
            && !IsFiatAssetSymbol(assetSymbol);
    }

    private static bool IsFiatAssetSymbol(string assetSymbol)
    {
        return assetSymbol.Trim().ToUpperInvariant() is "AUD" or "CAD" or "CHF" or "EUR" or "GBP" or "JPY" or "USD";
    }

    private static string NormalizeAsset(string assetSymbol)
    {
        return string.IsNullOrWhiteSpace(assetSymbol) ? "UNKNOWN" : assetSymbol.Trim().ToUpperInvariant();
    }

    private static decimal ParseDecimal(string value)
    {
        return decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);
    }

    private static string FormatDecimal(decimal value)
    {
        return value.ToString("0.############################", CultureInfo.InvariantCulture);
    }

    private sealed record NumberContribution(decimal Amount, LedgerEvent Event, int PostingIndex, LedgerPosting Posting);

    private sealed record TextContribution(string Text);

    private interface IUnknownTrace
    {
        List<LedgerEvent> UnknownEvents { get; }
    }

    private sealed record RwSnapshotTrace(
        string AssetSymbol,
        List<NumberContribution> Opening,
        List<NumberContribution> Closing,
        List<NumberContribution> Incoming,
        List<NumberContribution> Outgoing,
        List<LedgerEvent> UnknownEvents) : IUnknownTrace
    {
        public static RwSnapshotTrace Empty(string assetSymbol)
        {
            return new(assetSymbol, [], [], [], [], []);
        }
    }

    private sealed record RwValueTrace(
        string AssetSymbol,
        List<NumberContribution> Opening,
        List<NumberContribution> Closing,
        List<NumberContribution> IncomingValue,
        List<NumberContribution> OutgoingValue,
        List<NumberContribution> FeeValue,
        List<LedgerEvent> UnknownEvents,
        List<LedgerEvent> MissingValuePostings) : IUnknownTrace
    {
        public static RwValueTrace Empty(string assetSymbol)
        {
            return new(assetSymbol, [], [], [], [], [], [], []);
        }
    }
}
