namespace LedgerForge.Tax.Abstractions;

public sealed record TaxReportResult(
    TaxModuleDescriptor Module,
    int Year,
    IReadOnlyList<string> Warnings);
