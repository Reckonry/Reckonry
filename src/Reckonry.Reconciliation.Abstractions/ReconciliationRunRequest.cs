namespace Reckonry.Reconciliation.Abstractions;

public sealed record ReconciliationRunRequest(
    string OfficialReportsFolder,
    string ReckonryReportsFolder,
    string OutputFolder);
