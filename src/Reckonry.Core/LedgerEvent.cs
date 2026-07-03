namespace Reckonry.Core;

public sealed record LedgerEvent(
    Guid Id,
    DateTimeOffset TimestampUtc,
    LedgerEventType EventType,
    string Description,
    SourceReference SourceReference,
    IReadOnlyList<LedgerPosting> Postings);
