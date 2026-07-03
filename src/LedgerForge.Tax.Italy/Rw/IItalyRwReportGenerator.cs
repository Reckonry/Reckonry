using LedgerForge.Core;

namespace LedgerForge.Tax.Italy.Rw;

public interface IItalyRwReportGenerator
{
    ItalyRwReport GenerateCryptoDraft(
        int year,
        IReadOnlyCollection<LedgerEvent> ledgerEvents,
        ItalyRwReportConfiguration configuration,
        IReadOnlyCollection<ItalyRwAssetValuation> valuations);
}
