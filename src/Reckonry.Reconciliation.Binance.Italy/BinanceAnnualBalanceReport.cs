namespace Reckonry.Reconciliation.Binance.Italy;

public sealed record BinanceAnnualBalanceReport(
    BinanceReportMetadata Metadata,
    IReadOnlyDictionary<string, string> Fields)
    : BinanceReportDocument(Metadata, Fields);
