namespace Reckonry.Reconciliation.Coinbase;

public sealed record CoinbaseReconciliationDocumentSummary(
    string ReportType,
    int? Year,
    bool ExtractionSucceeded,
    int ExtractedFieldCount,
    string Status);
