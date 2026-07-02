namespace LedgerForge.Reconciliation;

public sealed record BinanceReconciliationSummary(
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<BinanceReconciliationDocumentSummary> Documents,
    IReadOnlyList<int> LedgerForgeSnapshotYears,
    IReadOnlyList<int> LedgerForgeValueYears);
