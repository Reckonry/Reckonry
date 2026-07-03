namespace Reckonry.Reconciliation.Binance.Italy;

public sealed record BinanceReconciliationSummary(
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<BinanceReconciliationDocumentSummary> Documents,
    IReadOnlyList<int> ReckonrySnapshotYears,
    IReadOnlyList<int> ReckonryValueYears);
