namespace Reckonry.Reconciliation.Abstractions;

public sealed record ReconciliationRunResult(
    string ModuleId,
    string OutputFolder,
    IReadOnlyList<string> GeneratedFileNames,
    object Summary);
