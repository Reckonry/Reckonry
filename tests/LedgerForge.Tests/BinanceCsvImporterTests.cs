using LedgerForge.Importers.Binance;
using LedgerForge.Core;

namespace LedgerForge.Tests;

public sealed class BinanceCsvImporterTests
{
    [Fact]
    public void ImportFolder_DoesNotCrashOnEmptyFolder()
    {
        var inputFolder = Directory.CreateTempSubdirectory("ledgerforge-binance-empty-");
        try
        {
            var importer = new BinanceCsvImporter();

            var events = importer.ImportFolder(inputFolder.FullName);

            Assert.Empty(events);
        }
        finally
        {
            inputFolder.Delete(recursive: true);
        }
    }

    [Fact]
    public void ImportFolder_ParsesDepositRows()
    {
        using var fixture = BinanceCsvFixture.Create(
            "deposits.csv",
            """
            UTC_Time,Account,Operation,Coin,Change,Remark
            2024-01-02 10:00:00,Spot,Deposit,BTC,0.50000000,Completed
            """);

        var ledgerEvent = ImportSingle(fixture);

        Assert.Equal(LedgerEventType.Deposit, ledgerEvent.EventType);
        Assert.Equal(new DateTimeOffset(2024, 1, 2, 10, 0, 0, TimeSpan.Zero), ledgerEvent.TimestampUtc);
        Assert.Equal("deposits.csv", ledgerEvent.SourceReference.SourceFile);
        Assert.Equal(2, ledgerEvent.SourceReference.SourceRowNumber);
        Assert.Equal("2024-01-02 10:00:00,Spot,Deposit,BTC,0.50000000,Completed", ledgerEvent.SourceReference.RawData);
        var posting = Assert.Single(ledgerEvent.Postings);
        Assert.Equal("BTC", posting.AssetSymbol);
        Assert.Equal(0.50000000m, posting.Amount);
        Assert.Equal(LedgerPostingDirection.In, posting.Direction);
    }

    [Fact]
    public void ImportFolder_ParsesWithdrawalRows()
    {
        using var fixture = BinanceCsvFixture.Create(
            "withdrawals.csv",
            """
            UTC_Time,Account,Operation,Coin,Change,Remark
            2024-01-03 11:30:00,Spot,Withdraw,ETH,-1.25000000,Completed
            """);

        var ledgerEvent = ImportSingle(fixture);

        Assert.Equal(LedgerEventType.Withdrawal, ledgerEvent.EventType);
        var posting = Assert.Single(ledgerEvent.Postings);
        Assert.Equal("ETH", posting.AssetSymbol);
        Assert.Equal(1.25000000m, posting.Amount);
        Assert.Equal(LedgerPostingDirection.Out, posting.Direction);
    }

    [Fact]
    public void ImportFolder_ParsesSpotTradeRows()
    {
        using var fixture = BinanceCsvFixture.Create(
            "spot-trades.csv",
            """
            Date(UTC),Market,Type,Price,Amount,Total,Fee,Fee Coin
            2024-01-04 12:45:00,BTCUSDT,BUY,40000.00,0.01000000,400.00,0.00001000,BTC
            """);

        var ledgerEvent = ImportSingle(fixture);

        Assert.Equal(LedgerEventType.Trade, ledgerEvent.EventType);
        Assert.Contains(ledgerEvent.Postings, p => p.AssetSymbol == "BTC" && p.Amount == 0.01000000m && p.Direction == LedgerPostingDirection.In);
        Assert.Contains(ledgerEvent.Postings, p => p.AssetSymbol == "USDT" && p.Amount == 400.00m && p.Direction == LedgerPostingDirection.Out);
        Assert.Contains(ledgerEvent.Postings, p => p.AssetSymbol == "BTC" && p.Amount == 0.00001000m && p.Direction == LedgerPostingDirection.Out);
    }

    [Fact]
    public void ImportFolder_ParsesConversionRows()
    {
        using var fixture = BinanceCsvFixture.Create(
            "conversions.csv",
            """
            Date,From Asset,From Amount,To Asset,To Amount,Fee,Fee Coin
            2024-01-05 09:15:00,BNB,2.00000000,USDT,600.00,0.01,BNB
            """);

        var ledgerEvent = ImportSingle(fixture);

        Assert.Equal(LedgerEventType.Conversion, ledgerEvent.EventType);
        Assert.Contains(ledgerEvent.Postings, p => p.AssetSymbol == "BNB" && p.Amount == 2.00000000m && p.Direction == LedgerPostingDirection.Out);
        Assert.Contains(ledgerEvent.Postings, p => p.AssetSymbol == "USDT" && p.Amount == 600.00m && p.Direction == LedgerPostingDirection.In);
        Assert.Contains(ledgerEvent.Postings, p => p.AssetSymbol == "BNB" && p.Amount == 0.01m && p.Direction == LedgerPostingDirection.Out);
    }

    [Fact]
    public void ImportFolder_ParsesRewardRows()
    {
        using var fixture = BinanceCsvFixture.Create(
            "rewards.csv",
            """
            UTC_Time,Account,Operation,Coin,Change,Remark
            2024-01-06 08:00:00,Earn,Simple Earn Flexible Interest,USDT,1.23450000,Daily reward
            """);

        var ledgerEvent = ImportSingle(fixture);

        Assert.Equal(LedgerEventType.Reward, ledgerEvent.EventType);
        var posting = Assert.Single(ledgerEvent.Postings);
        Assert.Equal("USDT", posting.AssetSymbol);
        Assert.Equal(1.23450000m, posting.Amount);
        Assert.Equal(LedgerPostingDirection.In, posting.Direction);
    }

    [Fact]
    public void ImportFolder_UnknownRowsPreserveRawCsv()
    {
        using var fixture = BinanceCsvFixture.Create(
            "unknown.csv",
            """
            Mystery Time,Kind,Payload
            2024-01-07 00:00:00,Something Else,"unrecognized,row,kept"
            """);

        var ledgerEvent = ImportSingle(fixture);

        Assert.Equal(LedgerEventType.Unknown, ledgerEvent.EventType);
        Assert.Equal("unknown.csv", ledgerEvent.SourceReference.SourceFile);
        Assert.Equal(2, ledgerEvent.SourceReference.SourceRowNumber);
        Assert.Equal("2024-01-07 00:00:00,Something Else,\"unrecognized,row,kept\"", ledgerEvent.SourceReference.RawData);
        Assert.Empty(ledgerEvent.Postings);
    }

    private static LedgerEvent ImportSingle(BinanceCsvFixture fixture)
    {
        var importer = new BinanceCsvImporter();

        return Assert.Single(importer.ImportFolder(fixture.InputFolder.FullName));
    }

    private sealed class BinanceCsvFixture : IDisposable
    {
        private BinanceCsvFixture(DirectoryInfo inputFolder)
        {
            InputFolder = inputFolder;
        }

        public DirectoryInfo InputFolder { get; }

        public static BinanceCsvFixture Create(string fileName, string contents)
        {
            var inputFolder = Directory.CreateTempSubdirectory("ledgerforge-binance-");
            File.WriteAllText(Path.Combine(inputFolder.FullName, fileName), contents.ReplaceLineEndings(Environment.NewLine));
            return new BinanceCsvFixture(inputFolder);
        }

        public void Dispose()
        {
            InputFolder.Delete(recursive: true);
        }
    }
}
