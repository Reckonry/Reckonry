namespace Reckonry.Audit;

public sealed record IntegrityFinding(
    string Code,
    IntegritySeverity Severity,
    string Category,
    string Message,
    int Count,
    string Recommendation);
