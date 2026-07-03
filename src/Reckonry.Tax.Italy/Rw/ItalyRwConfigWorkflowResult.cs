namespace Reckonry.Tax.Italy.Rw;

public sealed record ItalyRwConfigWorkflowResult(
    string GeneratedFileName,
    int TotalAssets,
    int FilledValuationCount,
    int RemainingMissingValuationCount,
    int WarningCount);
