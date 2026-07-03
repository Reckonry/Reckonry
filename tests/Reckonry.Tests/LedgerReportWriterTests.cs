using Reckonry.Core;
using Reckonry.Reports;

namespace Reckonry.Tests;

public sealed class LedgerReportWriterTests
{
    [Fact]
    public async Task WriteAsync_CanWriteLedgerJson()
    {
        var outputFolder = Directory.CreateTempSubdirectory("reckonry-report-");
        try
        {
            var ledgerPath = Path.Combine(outputFolder.FullName, "ledger.json");
            var events = new[]
            {
                new LedgerEvent(
                    Guid.NewGuid(),
                    DateTimeOffset.UnixEpoch,
                    LedgerEventType.Unknown,
                    "Unknown row",
                    new SourceReference("Binance", "sample.csv", 1, "raw,row"),
                    Array.Empty<LedgerPosting>())
            };

            var writer = new LedgerReportWriter();

            await writer.WriteAsync(ledgerPath, events);

            Assert.True(File.Exists(ledgerPath));
            Assert.Contains("\"schemaVersion\": \"reckonry-ledger-v1\"", await File.ReadAllTextAsync(ledgerPath));
            Assert.Contains("\"eventType\": \"Unknown\"", await File.ReadAllTextAsync(ledgerPath));
            Assert.True(File.Exists(Path.Combine(outputFolder.FullName, "exceptions.csv")));
            Assert.Contains("raw,row", await File.ReadAllTextAsync(Path.Combine(outputFolder.FullName, "exceptions.csv")));
        }
        finally
        {
            outputFolder.Delete(recursive: true);
        }
    }
}
