namespace LedgerForge.Core;

public sealed record LedgerMetadata(
    DateTimeOffset CreatedAtUtc,
    string Generator,
    int EventCount);
