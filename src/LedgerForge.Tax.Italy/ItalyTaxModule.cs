using LedgerForge.Tax.Abstractions;

namespace LedgerForge.Tax.Italy;

public sealed class ItalyTaxModule : ITaxModule
{
    public TaxModuleDescriptor Descriptor { get; } = new("IT", "Italy", "0.1.0");

    public TaxReportResult Analyze(TaxReportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new TaxReportResult(
            Descriptor,
            request.Year,
            new[]
            {
                "Italy tax module is a placeholder. It does not calculate taxes, capital gains, LIFO, FIFO, or legal advice."
            });
    }
}
