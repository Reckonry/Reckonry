using Reckonry.Core;
using Reckonry.Importers.Coinbase;

namespace Reckonry.Tests;

public sealed class CoinbaseCsvImporterTests
{
    [Fact]
    public void ImportFolder_DoesNotCrashOnEmptyFolder()
    {
        var inputFolder = Directory.CreateTempSubdirectory("reckonry-coinbase-empty-");
        try
        {
            var importer = new CoinbaseImporter();

            var events = importer.ImportFolder(inputFolder.FullName);

            Assert.Empty(events);
        }
        finally
        {
            inputFolder.Delete(recursive: true);
        }
    }

    [Fact]
    public void ImportFolder_ParsesBuyRows()
    {
        using var fixture = CoinbaseCsvFixture.Create(
            "transactions.csv",
            """
            Timestamp,Transaction Type,Asset,Quantity Transacted,Spot Price Currency,Spot Price at Transaction,Subtotal,Total (inclusive of fees and/or spread),Fees and/or Spread,Notes
            2025-02-01T10:00:00Z,Buy,BTC,0.01000000,EUR,40000.00,400.00,402.00,2.00,Synthetic buy
            """);

        var ledgerEvent = ImportSingle(fixture);

        Assert.Equal(LedgerEventType.Trade, ledgerEvent.EventType);
        Assert.Equal(new DateTimeOffset(2025, 2, 1, 10, 0, 0, TimeSpan.Zero), ledgerEvent.TimestampUtc);
        Assert.Equal("Coinbase", ledgerEvent.SourceReference.SourceSystem);
        Assert.Equal("transactions.csv", ledgerEvent.SourceReference.SourceFile);
        Assert.Contains(ledgerEvent.Postings, p => p.AssetSymbol == "BTC" && p.Amount == 0.01000000m && p.Direction == LedgerPostingDirection.In);
        Assert.Contains(ledgerEvent.Postings, p => p.AssetSymbol == "EUR" && p.Amount == 400.00m && p.Direction == LedgerPostingDirection.Out);
        Assert.Contains(ledgerEvent.Postings, p => p.AssetSymbol == "EUR" && p.Amount == 2.00m && p.Direction == LedgerPostingDirection.Out);
    }

    [Fact]
    public void ImportFolder_ParsesSellRows()
    {
        using var fixture = CoinbaseCsvFixture.Create(
            "transactions.csv",
            """
            Timestamp,Transaction Type,Asset,Quantity Transacted,Spot Price Currency,Spot Price at Transaction,Subtotal,Total (inclusive of fees and/or spread),Fees and/or Spread,Notes
            2025-02-02T11:00:00Z,Sell,ETH,0.25000000,EUR,2200.00,550.00,548.50,1.50,Synthetic sell
            """);

        var ledgerEvent = ImportSingle(fixture);

        Assert.Equal(LedgerEventType.Trade, ledgerEvent.EventType);
        Assert.Contains(ledgerEvent.Postings, p => p.AssetSymbol == "ETH" && p.Amount == 0.25000000m && p.Direction == LedgerPostingDirection.Out);
        Assert.Contains(ledgerEvent.Postings, p => p.AssetSymbol == "EUR" && p.Amount == 550.00m && p.Direction == LedgerPostingDirection.In);
        Assert.Contains(ledgerEvent.Postings, p => p.AssetSymbol == "EUR" && p.Amount == 1.50m && p.Direction == LedgerPostingDirection.Out);
    }

    [Fact]
    public void ImportFolder_ParsesReceiveRows()
    {
        using var fixture = CoinbaseCsvFixture.Create(
            "transactions.csv",
            """
            Timestamp,Transaction Type,Asset,Quantity Transacted,Spot Price Currency,Spot Price at Transaction,Subtotal,Total (inclusive of fees and/or spread),Fees and/or Spread,Notes
            2025-02-03T12:00:00Z,Receive,SOL,3.50000000,EUR,90.00,315.00,315.00,0.00,Synthetic receive
            """);

        var ledgerEvent = ImportSingle(fixture);

        Assert.Equal(LedgerEventType.Deposit, ledgerEvent.EventType);
        var posting = Assert.Single(ledgerEvent.Postings);
        Assert.Equal("SOL", posting.AssetSymbol);
        Assert.Equal(3.50000000m, posting.Amount);
        Assert.Equal(LedgerPostingDirection.In, posting.Direction);
    }

    [Fact]
    public void ImportFolder_ParsesRewardRows()
    {
        using var fixture = CoinbaseCsvFixture.Create(
            "transactions.csv",
            """
            Timestamp,Transaction Type,Asset,Quantity Transacted,Spot Price Currency,Spot Price at Transaction,Subtotal,Total (inclusive of fees and/or spread),Fees and/or Spread,Notes
            2025-02-04T13:00:00Z,Learning Reward,USDC,5.00000000,EUR,1.00,5.00,5.00,0.00,Synthetic learning reward
            """);

        var ledgerEvent = ImportSingle(fixture);

        Assert.Equal(LedgerEventType.Reward, ledgerEvent.EventType);
        Assert.Contains(ledgerEvent.Postings, p => p.AssetSymbol == "USDC" && p.Amount == 5.00000000m && p.Direction == LedgerPostingDirection.In);
    }

    [Fact]
    public void ImportFolder_ParsesNormalizedConversionRows()
    {
        using var fixture = CoinbaseCsvFixture.Create(
            "normalized.csv",
            """
            timestamp,type,asset,quantity,native_currency,native_amount,fee_amount,fee_currency,received_asset,received_quantity,notes
            2025-02-05T14:00:00Z,Convert,ETH,0.10000000,EUR,220.00,1.00,EUR,BTC,0.00500000,Synthetic conversion
            """);

        var ledgerEvent = ImportSingle(fixture);

        Assert.Equal(LedgerEventType.Conversion, ledgerEvent.EventType);
        Assert.Contains(ledgerEvent.Postings, p => p.AssetSymbol == "ETH" && p.Amount == 0.10000000m && p.Direction == LedgerPostingDirection.Out);
        Assert.Contains(ledgerEvent.Postings, p => p.AssetSymbol == "BTC" && p.Amount == 0.00500000m && p.Direction == LedgerPostingDirection.In);
        Assert.Contains(ledgerEvent.Postings, p => p.AssetSymbol == "EUR" && p.Amount == 1.00m && p.Direction == LedgerPostingDirection.Out);
    }

    [Fact]
    public void ImportFolder_UnknownRowsPreserveRawCsvAndTimestamp()
    {
        using var fixture = CoinbaseCsvFixture.Create(
            "unknown.csv",
            """
            Mystery Time,Kind,Payload
            2025-02-06T15:00:00Z,Unsupported,"synthetic,raw,row"
            """);

        var ledgerEvent = ImportSingle(fixture);

        Assert.Equal(LedgerEventType.Unknown, ledgerEvent.EventType);
        Assert.Equal(new DateTimeOffset(2025, 2, 6, 15, 0, 0, TimeSpan.Zero), ledgerEvent.TimestampUtc);
        Assert.Equal("unknown.csv", ledgerEvent.SourceReference.SourceFile);
        Assert.Equal(2, ledgerEvent.SourceReference.SourceRowNumber);
        Assert.Equal("2025-02-06T15:00:00Z,Unsupported,\"synthetic,raw,row\"", ledgerEvent.SourceReference.RawData);
        Assert.Empty(ledgerEvent.Postings);
    }

    [Fact]
    public void ImportFolder_DemoSampleImportsCanonicalLedgerEvents()
    {
        var root = FindRepositoryRoot();
        var importer = new CoinbaseImporter();

        var events = importer.ImportFolder(Path.Combine(root, "samples", "demo", "coinbase"));

        Assert.Equal(6, events.Count);
        Assert.Contains(events, e => e.EventType == LedgerEventType.Unknown);
        Assert.All(events, e => Assert.Equal("Coinbase", e.SourceReference.SourceSystem));
    }

    private static LedgerEvent ImportSingle(CoinbaseCsvFixture fixture)
    {
        var importer = new CoinbaseImporter();

        return Assert.Single(importer.ImportFolder(fixture.InputFolder.FullName));
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

    private sealed class CoinbaseCsvFixture : IDisposable
    {
        private CoinbaseCsvFixture(DirectoryInfo inputFolder)
        {
            InputFolder = inputFolder;
        }

        public DirectoryInfo InputFolder { get; }

        public static CoinbaseCsvFixture Create(string fileName, string contents)
        {
            var inputFolder = Directory.CreateTempSubdirectory("reckonry-coinbase-");
            File.WriteAllText(Path.Combine(inputFolder.FullName, fileName), contents.ReplaceLineEndings(Environment.NewLine));
            return new CoinbaseCsvFixture(inputFolder);
        }

        public void Dispose()
        {
            InputFolder.Delete(recursive: true);
        }
    }
}
