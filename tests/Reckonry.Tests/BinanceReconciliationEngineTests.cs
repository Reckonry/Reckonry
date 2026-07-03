using System.Text.Json;
using Reckonry.Reconciliation.Binance.Italy;

namespace Reckonry.Tests;

public sealed class BinanceReconciliationEngineTests
{
    [Fact]
    public async Task ReconcileAsync_WritesPrivacySafeSummaryFiles()
    {
        var root = Directory.CreateTempSubdirectory("reckonry-reconciliation-");
        try
        {
            var officialReports = Directory.CreateDirectory(Path.Combine(root.FullName, "official"));
            var ledgerReports = Directory.CreateDirectory(Path.Combine(root.FullName, "reckonry"));
            var output = Path.Combine(root.FullName, "output");

            File.WriteAllText(Path.Combine(officialReports.FullName, "annual.pdf"), BuildTextPdf("Binance Italy Annual Balance Report Year: 2025 Total EUR: 100.00"));
            File.WriteAllText(Path.Combine(ledgerReports.FullName, "rw-snapshot-2025.json"), "[]");
            File.WriteAllText(Path.Combine(ledgerReports.FullName, "rw-value-2025.json"), "[]");

            var engine = new BinanceReconciliationEngine();

            var summary = await engine.ReconcileAsync(officialReports.FullName, ledgerReports.FullName, output);

            Assert.Contains(summary.Documents, document => document.Status == ReconciliationStatus.MatchedForReview);
            Assert.Contains(summary.Documents, document => document.Status == ReconciliationStatus.MissingOfficialReport);
            Assert.True(File.Exists(Path.Combine(output, "reconciliation-summary.json")));
            Assert.True(File.Exists(Path.Combine(output, "reconciliation-summary.md")));

            var summaryJson = await File.ReadAllTextAsync(Path.Combine(output, "reconciliation-summary.json"));
            Assert.Contains("matchedForReview", summaryJson, StringComparison.OrdinalIgnoreCase);

            using var document = JsonDocument.Parse(summaryJson);
            Assert.False(document.RootElement.GetRawText().Contains("100.00", StringComparison.Ordinal));
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    private static string BuildTextPdf(string text)
    {
        var escaped = text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        var streamText = $"BT /F1 12 Tf 72 720 Td ({escaped}) Tj ET";

        return $"""
               %PDF-1.4
               1 0 obj
               << /Type /Catalog /Pages 2 0 R >>
               endobj
               2 0 obj
               << /Type /Pages /Kids [3 0 R] /Count 1 >>
               endobj
               3 0 obj
               << /Type /Page /Parent 2 0 R /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>
               endobj
               4 0 obj
               << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>
               endobj
               5 0 obj
               << /Length {streamText.Length} >>
               stream
               {streamText}
               endstream
               endobj
               trailer
               << /Root 1 0 R >>
               %%EOF
               """;
    }
}
