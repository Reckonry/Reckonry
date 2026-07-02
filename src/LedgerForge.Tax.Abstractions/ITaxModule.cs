namespace LedgerForge.Tax.Abstractions;

public interface ITaxModule
{
    TaxModuleDescriptor Descriptor { get; }

    TaxReportResult Analyze(TaxReportRequest request);
}
