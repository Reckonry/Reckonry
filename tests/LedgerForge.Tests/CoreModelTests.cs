using LedgerForge.Core;

namespace LedgerForge.Tests;

public sealed class CoreModelTests
{
    [Fact]
    public void AssetAmount_UsesDecimalAmount()
    {
        var amount = new AssetAmount("BTC", 0.123456789012345678m);

        Assert.IsType<decimal>(amount.Amount);
        Assert.Equal(0.123456789012345678m, amount.Amount);
    }

    [Fact]
    public void UnknownEvent_PreservesSourceReferenceRawData()
    {
        const string rawData = "Unsupported,CSV,Row";

        var ledgerEvent = new LedgerEvent(
            Guid.NewGuid(),
            DateTimeOffset.UnixEpoch,
            LedgerEventType.Unknown,
            "Unknown row",
            new SourceReference("Binance", "sample.csv", 42, rawData),
            Array.Empty<LedgerPosting>());

        Assert.Equal(LedgerEventType.Unknown, ledgerEvent.EventType);
        Assert.Equal(rawData, ledgerEvent.SourceReference.RawData);
    }
}
