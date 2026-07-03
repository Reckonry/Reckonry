namespace Reckonry.Tax.Abstractions;

public sealed record TaxReportResult(
    TaxModuleDescriptor Module,
    int Year,
    IReadOnlyList<string> Warnings);
