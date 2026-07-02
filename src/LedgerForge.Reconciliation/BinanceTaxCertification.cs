namespace LedgerForge.Reconciliation;

public sealed record BinanceTaxCertification(
    BinanceReportMetadata Metadata,
    IReadOnlyDictionary<string, string> Fields)
    : BinanceReportDocument(Metadata, Fields);
