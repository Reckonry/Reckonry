namespace Reckonry.Reconciliation;

public sealed record BinanceReconciliationSummary(
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<BinanceReconciliationDocumentSummary> Documents,
    IReadOnlyList<int> ReckonrySnapshotYears,
    IReadOnlyList<int> ReckonryValueYears);
