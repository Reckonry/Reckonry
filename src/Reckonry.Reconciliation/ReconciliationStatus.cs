namespace Reckonry.Reconciliation;

public enum ReconciliationStatus
{
    MatchedForReview,
    MissingReckonryReports,
    MissingOfficialReport,
    OcrRequired,
    NeedsManualReview
}
