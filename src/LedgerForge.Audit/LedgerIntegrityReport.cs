namespace LedgerForge.Audit;

public sealed record LedgerIntegrityReport(
    DateTimeOffset GeneratedAtUtc,
    int TotalEvents,
    int TotalPostings,
    int IntegrityScore,
    int ConfidenceScore,
    IReadOnlyList<IntegrityFinding> Findings,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Recommendations);
