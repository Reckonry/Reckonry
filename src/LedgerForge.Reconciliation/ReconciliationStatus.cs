namespace LedgerForge.Reconciliation;

public enum ReconciliationStatus
{
    MatchedForReview,
    MissingLedgerForgeReports,
    MissingOfficialReport,
    OcrRequired,
    NeedsManualReview
}
