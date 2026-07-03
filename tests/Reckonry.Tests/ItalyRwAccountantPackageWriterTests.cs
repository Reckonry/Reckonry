using Reckonry.Core;
using Reckonry.Tax.Italy.Rw;

namespace Reckonry.Tests;

public sealed class ItalyRwAccountantPackageWriterTests
{
    [Fact]
    public async Task WriteAsync_WritesMarkdownCsvAndJsonReviewPackage()
    {
        var root = Directory.CreateTempSubdirectory("reckonry-accountant-package-");
        try
        {
            var ledgerPath = Path.Combine(root.FullName, "ledger.json");
            await File.WriteAllTextAsync(ledgerPath, """{"schemaVersion":"test","events":[]}""");
            var outputFolder = Path.Combine(root.FullName, "accountant");
            var events = new[]
            {
                CreateEvent(LedgerEventType.Deposit, "BTC")
            };

            var result = await new ItalyRwAccountantPackageWriter().WriteAsync(
                ledgerPath,
                outputFolder,
                2025,
                events,
                ReportLanguages.English);

            Assert.Equal("NOT READY FOR FILING", result.ReadinessStatus);
            Assert.True(result.MissingInputCount > 0);
            Assert.True(result.WarningCount > 0);
            Assert.Contains("italy-rw-accountant-2025.md", result.GeneratedFileNames);
            Assert.Contains("italy-rw-accountant-2025.csv", result.GeneratedFileNames);
            Assert.Contains("italy-rw-accountant-2025.json", result.GeneratedFileNames);

            var markdown = await File.ReadAllTextAsync(Path.Combine(outputFolder, "italy-rw-accountant-2025.md"));
            var csv = await File.ReadAllTextAsync(Path.Combine(outputFolder, "italy-rw-accountant-2025.csv"));
            var json = await File.ReadAllTextAsync(Path.Combine(outputFolder, "italy-rw-accountant-2025.json"));

            Assert.Contains("NOT READY FOR FILING", markdown);
            Assert.Contains("Ledger SHA-256", markdown);
            Assert.Contains("Column3AssetCode", csv);
            Assert.Contains("BTC", csv);
            Assert.Contains("\"readinessStatus\": \"NOT READY FOR FILING\"", json);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task WriteAsync_IncludesBinanceReconciliationStatusWhenAvailable()
    {
        var root = Directory.CreateTempSubdirectory("reckonry-accountant-reconciliation-");
        try
        {
            var ledgerPath = Path.Combine(root.FullName, "ledger.json");
            await File.WriteAllTextAsync(ledgerPath, """{"schemaVersion":"test","events":[]}""");
            var reconciliationFolder = Directory.CreateDirectory(Path.Combine(root.FullName, "reconciliation"));
            await File.WriteAllTextAsync(
                Path.Combine(reconciliationFolder.FullName, "reconciliation-summary.json"),
                """
                {
                  "documents": [
                    {
                      "reportType": "ItalyAnnualBalanceReport",
                      "year": 2025,
                      "extractionSucceeded": true,
                      "extractedFieldCount": 4,
                      "status": "MatchedForReview"
                    }
                  ]
                }
                """);

            var outputFolder = Path.Combine(root.FullName, "accountant");

            await new ItalyRwAccountantPackageWriter().WriteAsync(
                ledgerPath,
                outputFolder,
                2025,
                new[] { CreateEvent(LedgerEventType.Deposit, "ETH") },
                ReportLanguages.English);

            var markdown = await File.ReadAllTextAsync(Path.Combine(outputFolder, "italy-rw-accountant-2025.md"));

            Assert.Contains("Binance Reconciliation", markdown);
            Assert.Contains("MatchedForReview", markdown);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    private static LedgerEvent CreateEvent(LedgerEventType eventType, string assetSymbol)
    {
        return new LedgerEvent(
            Guid.NewGuid(),
            new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            eventType,
            $"{eventType} {assetSymbol}",
            new SourceReference("Fake", "fake.csv", 1, "fake,row"),
            new[] { new LedgerPosting(assetSymbol, 1m, LedgerPostingDirection.In, "Fake:Account") });
    }
}
