using Reckonry.Core;
using Reckonry.Reports;

namespace Reckonry.Tests;

public sealed class RwSnapshotReportWriterTests
{
    [Fact]
    public void BuildRows_CalculatesOpeningMovementsAndClosingBalance()
    {
        var events = new[]
        {
            CreateEvent(new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.Zero), LedgerEventType.Deposit, new("BTC", 2.0m, LedgerPostingDirection.In, "Binance:Spot")),
            CreateEvent(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), LedgerEventType.Deposit, new("BTC", 0.5m, LedgerPostingDirection.In, "Binance:Spot")),
            CreateEvent(new DateTimeOffset(2025, 12, 31, 23, 59, 59, TimeSpan.Zero), LedgerEventType.Withdrawal, new("BTC", 0.25m, LedgerPostingDirection.Out, "Binance:Spot")),
            CreateEvent(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), LedgerEventType.Deposit, new("BTC", 10m, LedgerPostingDirection.In, "Binance:Spot"))
        };

        var btc = Assert.Single(RwSnapshotReportWriter.BuildRows(2025, events));

        Assert.Equal("BTC", btc.AssetSymbol);
        Assert.Equal(2.0m, btc.OpeningQuantity);
        Assert.Equal(0.5m, btc.IncomingQuantity);
        Assert.Equal(0.25m, btc.OutgoingQuantity);
        Assert.Equal(2.25m, btc.ClosingQuantity);
    }

    [Fact]
    public void BuildRows_UnknownEventsWarnAndDoNotAffectBalances()
    {
        var events = new[]
        {
            CreateEvent(new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero), LedgerEventType.Deposit, new("ETH", 1m, LedgerPostingDirection.In, "Binance:Spot")),
            CreateEvent(new DateTimeOffset(2025, 3, 1, 0, 0, 0, TimeSpan.Zero), LedgerEventType.Unknown, new("ETH", 99m, LedgerPostingDirection.In, "Binance:Spot")),
            CreateUnknownEventWithoutPostings(new DateTimeOffset(2025, 4, 1, 0, 0, 0, TimeSpan.Zero))
        };

        var rows = RwSnapshotReportWriter.BuildRows(2025, events);
        var eth = Assert.Single(rows, row => row.AssetSymbol == "ETH");
        var unknown = Assert.Single(rows, row => row.AssetSymbol == "UNKNOWN");

        Assert.Equal(1m, eth.ClosingQuantity);
        Assert.Equal(1, eth.UnknownEventCount);
        Assert.Contains("may affect balances", eth.Warning);
        Assert.Equal(1, unknown.UnknownEventCount);
        Assert.Contains("without asset postings", unknown.Warning);
    }

    [Fact]
    public void BuildRows_ExcludesZeroBalanceAssetsUnlessTheyHadActivity()
    {
        var events = new[]
        {
            CreateEvent(new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero), LedgerEventType.Deposit, new("DOGE", 10m, LedgerPostingDirection.In, "Binance:Spot")),
            CreateEvent(new DateTimeOffset(2024, 6, 2, 0, 0, 0, TimeSpan.Zero), LedgerEventType.Withdrawal, new("DOGE", 10m, LedgerPostingDirection.Out, "Binance:Spot")),
            CreateEvent(new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero), LedgerEventType.Deposit, new("ADA", 5m, LedgerPostingDirection.In, "Binance:Spot")),
            CreateEvent(new DateTimeOffset(2025, 6, 2, 0, 0, 0, TimeSpan.Zero), LedgerEventType.Withdrawal, new("ADA", 5m, LedgerPostingDirection.Out, "Binance:Spot"))
        };

        var rows = RwSnapshotReportWriter.BuildRows(2025, events);

        Assert.DoesNotContain(rows, row => row.AssetSymbol == "DOGE");
        var ada = Assert.Single(rows, row => row.AssetSymbol == "ADA");
        Assert.Equal(0m, ada.OpeningQuantity);
        Assert.Equal(0m, ada.ClosingQuantity);
        Assert.Equal(5m, ada.IncomingQuantity);
        Assert.Equal(5m, ada.OutgoingQuantity);
    }

    [Fact]
    public async Task WriteAsync_WritesRwSnapshotCsvAndJson()
    {
        var outputFolder = Directory.CreateTempSubdirectory("reckonry-rw-snapshot-");
        try
        {
            var events = new[]
            {
                CreateEvent(new DateTimeOffset(2025, 1, 2, 0, 0, 0, TimeSpan.Zero), LedgerEventType.Deposit, new("USDT", 100m, LedgerPostingDirection.In, "Binance:Spot"))
            };
            var writer = new RwSnapshotReportWriter();

            await writer.WriteAsync(outputFolder.FullName, 2025, events);

            var csvPath = Path.Combine(outputFolder.FullName, "rw-snapshot-2025.csv");
            var jsonPath = Path.Combine(outputFolder.FullName, "rw-snapshot-2025.json");

            Assert.True(File.Exists(csvPath));
            Assert.True(File.Exists(jsonPath));
            Assert.Contains("AssetSymbol,OpeningQuantity,ClosingQuantity,IncomingQuantity,OutgoingQuantity,UnknownEventCount,Warning", await File.ReadAllTextAsync(csvPath));
            Assert.Contains("USDT,0,100,100,0,0,", await File.ReadAllTextAsync(csvPath));
            Assert.Contains("\"assetSymbol\": \"USDT\"", await File.ReadAllTextAsync(jsonPath));
        }
        finally
        {
            outputFolder.Delete(recursive: true);
        }
    }

    private static LedgerEvent CreateEvent(DateTimeOffset timestamp, LedgerEventType eventType, LedgerPosting posting)
    {
        return new LedgerEvent(
            Guid.NewGuid(),
            timestamp,
            eventType,
            $"{eventType} {posting.AssetSymbol}",
            new SourceReference("Test", "test.csv", 1, "raw"),
            new[] { posting });
    }

    private static LedgerEvent CreateUnknownEventWithoutPostings(DateTimeOffset timestamp)
    {
        return new LedgerEvent(
            Guid.NewGuid(),
            timestamp,
            LedgerEventType.Unknown,
            "Unknown row",
            new SourceReference("Test", "unknown.csv", 2, "raw unknown"),
            Array.Empty<LedgerPosting>());
    }
}
