namespace Reckonry.Tax.Italy.Rw;

public sealed record ItalyRwAccountantPackageResult(
    IReadOnlyList<string> GeneratedFileNames,
    string ReadinessStatus,
    int MissingInputCount,
    int WarningCount);
