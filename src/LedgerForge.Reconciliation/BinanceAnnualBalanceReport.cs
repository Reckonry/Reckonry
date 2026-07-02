namespace LedgerForge.Reconciliation;

public sealed record BinanceAnnualBalanceReport(
    BinanceReportMetadata Metadata,
    IReadOnlyDictionary<string, string> Fields)
    : BinanceReportDocument(Metadata, Fields);
