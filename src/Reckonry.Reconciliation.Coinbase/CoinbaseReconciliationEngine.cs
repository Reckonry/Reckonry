using System.Globalization;
using System.Text;
using System.Text.Json;
using Reckonry.Reconciliation.Abstractions;

namespace Reckonry.Reconciliation.Coinbase;

public sealed class CoinbaseReconciliationEngine : IReconciliationModule
{
    public ReconciliationModuleDescriptor Descriptor { get; } = new(
        "coinbase-global",
        "Coinbase Global Reconciliation",
        ReconciliationScope.Provider,
        ProviderId: "coinbase",
        CountryCode: null,
        ProfessionalReviewRequired: false,
        SupportedInputFormats: ["csv"],
        GeneratedArtifacts: ["reconciliation-summary.json", "reconciliation-summary.md"]);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<ReconciliationRunResult> ReconcileAsync(
        ReconciliationRunRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OfficialReportsFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ReckonryReportsFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OutputFolder);

        var statement = ReadStatementSummary(request.OfficialReportsFolder);
        var ledger = await ReadLedgerSummaryAsync(request.ReckonryReportsFolder, cancellationToken);
        var status = BuildStatus(statement, ledger);
        var document = new CoinbaseReconciliationDocumentSummary(
            "CoinbaseSyntheticStatementSummary",
            statement.Year,
            statement.Found,
            statement.ExtractedFieldCount,
            status);

        var summary = new CoinbaseReconciliationSummary(
            DateTimeOffset.UtcNow,
            ledger.EventCount,
            ledger.UnknownEventCount,
            statement.ExpectedImportedRows,
            statement.ExpectedUnknownRows,
            [document]);

        Directory.CreateDirectory(request.OutputFolder);

        var jsonPath = Path.Combine(request.OutputFolder, "reconciliation-summary.json");
        var markdownPath = Path.Combine(request.OutputFolder, "reconciliation-summary.md");

        await using (var jsonStream = File.Create(jsonPath))
        {
            await JsonSerializer.SerializeAsync(jsonStream, summary, JsonOptions, cancellationToken);
        }

        await File.WriteAllTextAsync(markdownPath, BuildMarkdown(summary), cancellationToken);

        return new ReconciliationRunResult(
            Descriptor.Id,
            request.OutputFolder,
            ["reconciliation-summary.json", "reconciliation-summary.md"],
            summary);
    }

    private static string BuildStatus(StatementSummary statement, LedgerSummary ledger)
    {
        if (!statement.Found)
        {
            return "MissingOfficialStatement";
        }

        if (!ledger.Found)
        {
            return "MissingLedger";
        }

        if (statement.ExpectedImportedRows != ledger.EventCount
            || statement.ExpectedUnknownRows != ledger.UnknownEventCount)
        {
            return "NeedsManualReview";
        }

        return "MatchedForReview";
    }

    private static async Task<LedgerSummary> ReadLedgerSummaryAsync(
        string reckonryReportsFolder,
        CancellationToken cancellationToken)
    {
        var ledgerPath = Path.Combine(reckonryReportsFolder, "ledger.json");
        if (!File.Exists(ledgerPath))
        {
            return new LedgerSummary(false, 0, 0);
        }

        await using var stream = File.OpenRead(ledgerPath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("events", out var events)
            || events.ValueKind != JsonValueKind.Array)
        {
            return new LedgerSummary(false, 0, 0);
        }

        var eventCount = events.GetArrayLength();
        var unknownCount = events
            .EnumerateArray()
            .Count(item =>
                item.TryGetProperty("eventType", out var eventType)
                && string.Equals(eventType.GetString(), "Unknown", StringComparison.Ordinal));

        return new LedgerSummary(true, eventCount, unknownCount);
    }

    private static StatementSummary ReadStatementSummary(string officialReportsFolder)
    {
        if (!Directory.Exists(officialReportsFolder))
        {
            return StatementSummary.Missing;
        }

        var statementPath = Directory
            .EnumerateFiles(officialReportsFolder, "*.csv", SearchOption.TopDirectoryOnly)
            .Order(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (statementPath is null)
        {
            return StatementSummary.Missing;
        }

        var rows = File.ReadLines(statementPath)
            .Where(row => !string.IsNullOrWhiteSpace(row))
            .ToArray();

        if (rows.Length < 2)
        {
            return new StatementSummary(true, null, null, null, 0);
        }

        var header = SplitCsv(rows[0]);
        var values = SplitCsv(rows[1]);
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < header.Count; i++)
        {
            fields[header[i]] = i < values.Count ? values[i] : string.Empty;
        }

        return new StatementSummary(
            true,
            TryGetInt(fields, "year"),
            TryGetInt(fields, "expectedImportedRows"),
            TryGetInt(fields, "expectedUnknownRows"),
            fields.Count);
    }

    private static int? TryGetInt(IReadOnlyDictionary<string, string> fields, string key)
    {
        return fields.TryGetValue(key, out var value)
            && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
    }

    private static IReadOnlyList<string> SplitCsv(string row)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < row.Length; i++)
        {
            var currentChar = row[i];
            if (currentChar == '"')
            {
                if (inQuotes && i + 1 < row.Length && row[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (currentChar == ',' && !inQuotes)
            {
                values.Add(current.ToString().Trim());
                current.Clear();
                continue;
            }

            current.Append(currentChar);
        }

        values.Add(current.ToString().Trim());
        return values;
    }

    private static string BuildMarkdown(CoinbaseReconciliationSummary summary)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Coinbase Reconciliation Summary");
        builder.AppendLine();
        builder.AppendLine("This provider-level summary compares synthetic Coinbase statement metadata with generated Reckonry ledger counts. It intentionally excludes balances, quantities, transaction values, addresses, and raw source rows.");
        builder.AppendLine();
        builder.AppendLine("| Report Type | Year | Extraction | Fields | Status |");
        builder.AppendLine("| --- | ---: | --- | ---: | --- |");

        foreach (var document in summary.Documents)
        {
            builder
                .Append("| ")
                .Append(document.ReportType)
                .Append(" | ")
                .Append(document.Year?.ToString(CultureInfo.InvariantCulture) ?? "Unknown")
                .Append(" | ")
                .Append(document.ExtractionSucceeded ? "Succeeded" : "Failed")
                .Append(" | ")
                .Append(document.ExtractedFieldCount)
                .Append(" | ")
                .Append(document.Status)
                .AppendLine(" |");
        }

        builder.AppendLine();
        builder.AppendLine($"Ledger events: {summary.LedgerEventCount}");
        builder.AppendLine($"Unknown events: {summary.UnknownEventCount}");
        builder.AppendLine($"Expected imported rows: {summary.ExpectedImportedRows?.ToString(CultureInfo.InvariantCulture) ?? "Unknown"}");
        builder.AppendLine($"Expected unknown rows: {summary.ExpectedUnknownRows?.ToString(CultureInfo.InvariantCulture) ?? "Unknown"}");

        return builder.ToString();
    }

    private sealed record LedgerSummary(bool Found, int EventCount, int UnknownEventCount);

    private sealed record StatementSummary(
        bool Found,
        int? Year,
        int? ExpectedImportedRows,
        int? ExpectedUnknownRows,
        int ExtractedFieldCount)
    {
        public static StatementSummary Missing { get; } = new(false, null, null, null, 0);
    }
}
