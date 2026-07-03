namespace Reckonry.Core;

public sealed record SourceReference(
    string SourceSystem,
    string SourceFile,
    int SourceRowNumber,
    string RawData);
