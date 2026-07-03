using Reckonry.Core;

namespace Reckonry.Tax.Italy.Rw;

public interface IItalyRwReportGenerator
{
    ItalyRwReport GenerateCryptoDraft(
        int year,
        IReadOnlyCollection<LedgerEvent> ledgerEvents,
        ItalyRwReportConfiguration configuration,
        IReadOnlyCollection<ItalyRwAssetValuation> valuations);
}
