namespace LedgerForge.Tax.Italy.Rw;

public sealed record TaxDossierPdfResult(
    string GeneratedFileName,
    string ReadinessStatus,
    string Language,
    string Title,
    int SourceFileCount,
    int ImportedRowCount,
    int LedgerEventCount,
    int UnknownEventCount,
    int OfficialReportDocumentCount,
    int MissingValuationEvidenceCount,
    int ValidationErrorCount,
    int WarningCount);
