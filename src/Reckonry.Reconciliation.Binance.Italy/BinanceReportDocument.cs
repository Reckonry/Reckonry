namespace Reckonry.Reconciliation.Binance.Italy;

public abstract record BinanceReportDocument(
    BinanceReportMetadata Metadata,
    IReadOnlyDictionary<string, string> Fields);
