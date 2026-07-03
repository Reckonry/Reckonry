namespace Reckonry.Core;

public sealed record Ledger(
    string SchemaVersion,
    LedgerMetadata Metadata,
    IReadOnlyList<LedgerEvent> Events);
