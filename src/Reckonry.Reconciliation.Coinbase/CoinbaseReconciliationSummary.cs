namespace Reckonry.Reconciliation.Coinbase;

public sealed record CoinbaseReconciliationSummary(
    DateTimeOffset GeneratedUtc,
    int LedgerEventCount,
    int UnknownEventCount,
    int? ExpectedImportedRows,
    int? ExpectedUnknownRows,
    IReadOnlyList<CoinbaseReconciliationDocumentSummary> Documents);
