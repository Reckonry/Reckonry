using LedgerForge.Core;
using LedgerForge.Reports;

namespace LedgerForge.Tests;

public sealed class RwValueReportWriterTests
{
    [Fact]
    public void BuildRows_CalculatesQuantitiesAndEurValues()
    {
        var events = new[]
        {
            CreateEvent(new DateTimeOffset(2024, 12, 31, 23, 0, 0, TimeSpan.Zero), LedgerEventType.Deposit, new("BTC", 1m, LedgerPostingDirection.In, "Binance:Received")),
            CreateEvent(new DateTimeOffset(2025, 1, 2, 0, 0, 0, TimeSpan.Zero), LedgerEventType.Deposit, new("BTC", 0.5m, LedgerPostingDirection.In, "Binance:Received", new MoneyAmount("EUR", 100m))),
            CreateEvent(new DateTimeOffset(2025, 1, 3, 0, 0, 0, TimeSpan.Zero), LedgerEventType.Withdrawal, new("BTC", 0.25m, LedgerPostingDirection.Out, "Binance:Sent", new MoneyAmount("EUR", 50m))),
            CreateEvent(new DateTimeOffset(2025, 1, 3, 0, 0, 0, TimeSpan.Zero), LedgerEventType.Fee, new("BTC", 0.01m, LedgerPostingDirection.Out, "Binance:Fees", new MoneyAmount("EUR", 2m)))
        };

        var row = Assert.Single(RwValueReportWriter.BuildRows(2025, events));

        Assert.Equal("BTC", row.AssetSymbol);
        Assert.Equal(1m, row.OpeningQuantity);
        Assert.Equal(1.24m, row.ClosingQuantity);
        Assert.Equal(100m, row.IncomingValueEUR);
        Assert.Equal(50m, row.OutgoingValueEUR);
        Assert.Equal(2m, row.FeeValueEUR);
        Assert.Empty(row.Warning);
    }

    [Fact]
    public void BuildRows_WarnsForMissingValuesAndUnknownEvents()
    {
        var events = new[]
        {
            CreateEvent(new DateTimeOffset(2025, 1, 2, 0, 0, 0, TimeSpan.Zero), LedgerEventType.Deposit, new("ETH", 1m, LedgerPostingDirection.In, "Binance:Received")),
            CreateEvent(new DateTimeOffset(2025, 1, 3, 0, 0, 0, TimeSpan.Zero), LedgerEventType.Unknown, new("ETH", 1m, LedgerPostingDirection.In, "Binance:Received"))
        };

        var row = Assert.Single(RwValueReportWriter.BuildRows(2025, events));

        Assert.Equal("ETH", row.AssetSymbol);
        Assert.Contains("Unknown events", row.Warning);
        Assert.Contains("do not include EUR values", row.Warning);
    }

    [Fact]
    public async Task WriteAsync_WritesRwValueCsvAndJson()
    {
        var outputFolder = Directory.CreateTempSubdirectory("ledgerforge-rw-value-");
        try
        {
            var events = new[]
            {
                CreateEvent(new DateTimeOffset(2025, 1, 2, 0, 0, 0, TimeSpan.Zero), LedgerEventType.Deposit, new("USDT", 10m, LedgerPostingDirection.In, "Binance:Received", new MoneyAmount("EUR", 10m)))
            };
            var writer = new RwValueReportWriter();

            await writer.WriteAsync(outputFolder.FullName, 2025, events);

            var csvPath = Path.Combine(outputFolder.FullName, "rw-value-2025.csv");
            var jsonPath = Path.Combine(outputFolder.FullName, "rw-value-2025.json");

            Assert.True(File.Exists(csvPath));
            Assert.True(File.Exists(jsonPath));
            Assert.Contains("AssetSymbol,OpeningQuantity,ClosingQuantity,IncomingValueEUR,OutgoingValueEUR,FeeValueEUR,Warning", await File.ReadAllTextAsync(csvPath));
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
}
