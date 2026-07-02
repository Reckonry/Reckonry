namespace LedgerForge.Core;

public sealed record SourceReference(
    string SourceSystem,
    string SourceFile,
    int SourceRowNumber,
    string RawData);
