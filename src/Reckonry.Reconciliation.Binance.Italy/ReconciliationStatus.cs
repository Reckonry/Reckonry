namespace Reckonry.Reconciliation.Binance.Italy;

public enum ReconciliationStatus
{
    MatchedForReview,
    MissingReckonryReports,
    MissingOfficialReport,
    OcrRequired,
    NeedsManualReview
}
