using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace LedgerForge.Reconciliation;

public sealed partial class BinanceReconciliationEngine(
    IBinanceReportReader? reportReader = null)
    : IBinanceReconciliationEngine
{
    private readonly IBinanceReportReader _reportReader = reportReader ?? new BinanceReportReader();

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<BinanceReconciliationSummary> ReconcileAsync(
        string officialReportsFolder,
        string ledgerForgeReportsFolder,
        string outputFolder,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(officialReportsFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(ledgerForgeReportsFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFolder);

        var documents = _reportReader.ReadFolder(officialReportsFolder);
        var snapshotYears = DetectReportYears(ledgerForgeReportsFolder, "rw-snapshot-*.json");
        var valueYears = DetectReportYears(ledgerForgeReportsFolder, "rw-value-*.json");

        var documentSummaries = documents
            .Select(document => BuildDocumentSummary(document, snapshotYears, valueYears))
            .ToArray();

        var summaries = documentSummaries
            .Concat(BuildMissingOfficialReportSummaries(documentSummaries, snapshotYears, valueYears))
            .OrderBy(summary => summary.Year ?? int.MaxValue)
            .ThenBy(summary => summary.ReportType)
            .ToArray();

        var summary = new BinanceReconciliationSummary(
            DateTimeOffset.UtcNow,
            summaries,
            snapshotYears,
            valueYears);

        Directory.CreateDirectory(outputFolder);

        var jsonPath = Path.Combine(outputFolder, "reconciliation-summary.json");
        var markdownPath = Path.Combine(outputFolder, "reconciliation-summary.md");

        await using (var jsonStream = File.Create(jsonPath))
        {
            await JsonSerializer.SerializeAsync(jsonStream, summary, JsonOptions, cancellationToken);
        }

        await File.WriteAllTextAsync(markdownPath, BuildMarkdown(summary), cancellationToken);
        return summary;
    }

    private static BinanceReconciliationDocumentSummary BuildDocumentSummary(
        BinanceReportDocument document,
        IReadOnlyCollection<int> snapshotYears,
        IReadOnlyCollection<int> valueYears)
    {
        var metadata = document.Metadata;

        var status = metadata switch
        {
            { IsImageOnly: true } => ReconciliationStatus.OcrRequired,
            { ReportType: BinanceReportType.Unknown } => ReconciliationStatus.NeedsManualReview,
            { TaxYear: null } => ReconciliationStatus.NeedsManualReview,
            _ when !snapshotYears.Contains(metadata.TaxYear!.Value) || !valueYears.Contains(metadata.TaxYear!.Value)
                => ReconciliationStatus.MissingLedgerForgeReports,
            _ => ReconciliationStatus.MatchedForReview
        };

        return new BinanceReconciliationDocumentSummary(
            metadata.ReportType,
            metadata.TaxYear,
            metadata.DocumentLanguage,
            metadata.PageCount,
            metadata.ExtractionSucceeded,
            metadata.IsImageOnly,
            document.Fields.Count,
            status);
    }

    private static IEnumerable<BinanceReconciliationDocumentSummary> BuildMissingOfficialReportSummaries(
        IReadOnlyCollection<BinanceReconciliationDocumentSummary> documentSummaries,
        IReadOnlyCollection<int> snapshotYears,
        IReadOnlyCollection<int> valueYears)
    {
        var ledgerYears = snapshotYears.Concat(valueYears).Distinct().Order();
        var expectedTypes = new[]
        {
            BinanceReportType.ItalyAnnualBalanceReport,
            BinanceReportType.ItalyTaxCertification
        };

        foreach (var year in ledgerYears)
        {
            foreach (var reportType in expectedTypes)
            {
                var exists = documentSummaries.Any(summary =>
                    summary.Year == year
                    && summary.ReportType == reportType
                    && summary.ExtractionSucceeded);

                if (exists)
                {
                    continue;
                }

                yield return new BinanceReconciliationDocumentSummary(
                    reportType,
                    year,
                    "Unknown",
                    0,
                    false,
                    false,
                    0,
                    ReconciliationStatus.MissingOfficialReport);
            }
        }
    }

    private static IReadOnlyList<int> DetectReportYears(string folder, string searchPattern)
    {
        if (!Directory.Exists(folder))
        {
            return Array.Empty<int>();
        }

        return Directory
            .EnumerateFiles(folder, searchPattern, SearchOption.TopDirectoryOnly)
            .Select(path => YearFromFileNameRegex().Match(Path.GetFileName(path)))
            .Where(match => match.Success)
            .Select(match => int.Parse(match.Groups[1].Value))
            .Distinct()
            .Order()
            .ToArray();
    }

    private static string BuildMarkdown(BinanceReconciliationSummary summary)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Binance Reconciliation Summary");
        builder.AppendLine();
        builder.AppendLine("This validation summary intentionally excludes balances, quantities, transaction values, addresses, and raw source rows.");
        builder.AppendLine();
        builder.AppendLine("| Report Type | Year | Language | Pages | Extraction | Fields | Status |");
        builder.AppendLine("| --- | ---: | --- | ---: | --- | ---: | --- |");

        foreach (var document in summary.Documents)
        {
            builder
                .Append("| ")
                .Append(document.ReportType)
                .Append(" | ")
                .Append(document.Year?.ToString() ?? "Unknown")
                .Append(" | ")
                .Append(document.DocumentLanguage)
                .Append(" | ")
                .Append(document.PageCount)
                .Append(" | ")
                .Append(document.ExtractionSucceeded ? "Succeeded" : document.OcrRequired ? "OCR required" : "Failed")
                .Append(" | ")
                .Append(document.ExtractedFieldCount)
                .Append(" | ")
                .Append(document.Status)
                .AppendLine(" |");
        }

        return builder.ToString();
    }

    [GeneratedRegex(@"(\d{4})")]
    private static partial Regex YearFromFileNameRegex();
}
