using Reckonry.Reconciliation.Abstractions;
using Reckonry.Reconciliation.Coinbase;
using Reckonry.Storage;

namespace Reckonry.Tests;

public sealed class CoinbaseReconciliationEngineTests
{
    [Fact]
    public async Task ReconcileAsync_MatchesSyntheticStatementToLedgerCounts()
    {
        var root = Directory.CreateTempSubdirectory("reckonry-coinbase-reconciliation-");
        try
        {
            var officialReports = Directory.CreateDirectory(Path.Combine(root.FullName, "official"));
            var reckonryReports = Directory.CreateDirectory(Path.Combine(root.FullName, "reckonry"));
            var output = Path.Combine(root.FullName, "reconciliation");

            await File.WriteAllTextAsync(
                Path.Combine(officialReports.FullName, "statement-summary.csv"),
                """
                provider,year,expectedImportedRows,expectedUnknownRows,notes
                Coinbase,2025,2,1,Synthetic test summary
                """);

            await File.WriteAllTextAsync(
                Path.Combine(reckonryReports.FullName, "ledger.json"),
                """
                {
                  "events": [
                    { "eventType": "Trade" },
                    { "eventType": "Unknown" }
                  ]
                }
                """);

            var result = await new CoinbaseReconciliationEngine().ReconcileAsync(
                new ReconciliationRunRequest(officialReports.FullName, reckonryReports.FullName, output));

            var summary = Assert.IsType<CoinbaseReconciliationSummary>(result.Summary);
            var document = Assert.Single(summary.Documents);

            Assert.Equal("coinbase-global", result.ModuleId);
            Assert.Equal("MatchedForReview", document.Status);
            Assert.Equal(2, summary.LedgerEventCount);
            Assert.Equal(1, summary.UnknownEventCount);
            Assert.True(File.Exists(Path.Combine(output, "reconciliation-summary.json")));
            Assert.True(File.Exists(Path.Combine(output, "reconciliation-summary.md")));
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task ReconcileAsync_FlagsMismatchedCountsForReview()
    {
        var root = Directory.CreateTempSubdirectory("reckonry-coinbase-reconciliation-mismatch-");
        try
        {
            var officialReports = Directory.CreateDirectory(Path.Combine(root.FullName, "official"));
            var reckonryReports = Directory.CreateDirectory(Path.Combine(root.FullName, "reckonry"));

            await File.WriteAllTextAsync(
                Path.Combine(officialReports.FullName, "statement-summary.csv"),
                """
                provider,year,expectedImportedRows,expectedUnknownRows,notes
                Coinbase,2025,99,0,Synthetic mismatch
                """);

            await File.WriteAllTextAsync(
                Path.Combine(reckonryReports.FullName, "ledger.json"),
                """
                {
                  "events": [
                    { "eventType": "Trade" }
                  ]
                }
                """);

            var result = await new CoinbaseReconciliationEngine().ReconcileAsync(
                new ReconciliationRunRequest(officialReports.FullName, reckonryReports.FullName, Path.Combine(root.FullName, "out")));

            var summary = Assert.IsType<CoinbaseReconciliationSummary>(result.Summary);
            var document = Assert.Single(summary.Documents);

            Assert.Equal("NeedsManualReview", document.Status);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task DemoSyntheticStatementReconcilesAgainstDemoLedger()
    {
        var root = FindRepositoryRoot();
        var work = Directory.CreateTempSubdirectory("reckonry-coinbase-demo-reconciliation-");
        try
        {
            var ledgerPath = Path.Combine(work.FullName, "ledger.json");
            var output = Path.Combine(work.FullName, "reconciliation");
            var importer = new Reckonry.Importers.Coinbase.CoinbaseImporter();
            var events = importer.ImportFolder(Path.Combine(root, "samples", "demo", "coinbase"));

            await new JsonLedgerStore().WriteAsync(ledgerPath, events);

            var result = await new CoinbaseReconciliationEngine().ReconcileAsync(
                new ReconciliationRunRequest(
                    Path.Combine(root, "samples", "demo", "coinbase-official-reports"),
                    work.FullName,
                    output));

            var summary = Assert.IsType<CoinbaseReconciliationSummary>(result.Summary);
            Assert.Equal("MatchedForReview", Assert.Single(summary.Documents).Status);
        }
        finally
        {
            work.Delete(recursive: true);
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Reckonry.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
