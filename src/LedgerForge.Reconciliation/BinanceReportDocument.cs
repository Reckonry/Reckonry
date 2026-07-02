namespace LedgerForge.Reconciliation;

public abstract record BinanceReportDocument(
    BinanceReportMetadata Metadata,
    IReadOnlyDictionary<string, string> Fields);
