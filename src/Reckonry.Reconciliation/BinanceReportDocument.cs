namespace Reckonry.Reconciliation;

public abstract record BinanceReportDocument(
    BinanceReportMetadata Metadata,
    IReadOnlyDictionary<string, string> Fields);
