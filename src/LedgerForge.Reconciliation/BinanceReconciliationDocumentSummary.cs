namespace LedgerForge.Reconciliation;

public sealed record BinanceReconciliationDocumentSummary(
    BinanceReportType ReportType,
    int? Year,
    string DocumentLanguage,
    int PageCount,
    bool ExtractionSucceeded,
    bool OcrRequired,
    int ExtractedFieldCount,
    ReconciliationStatus Status);
