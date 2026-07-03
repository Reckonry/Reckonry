using Reckonry.Core;

namespace Reckonry.Tax.Italy.Rw;

public interface IItalyRwConfigWorkflow
{
    Task<ItalyRwConfigWorkflowResult> WriteTemplateAsync(
        int year,
        IReadOnlyCollection<LedgerEvent> ledgerEvents,
        string outputPath,
        CancellationToken cancellationToken = default);

    Task<ItalyRwConfigWorkflowResult> FillFromBinanceAsync(
        string configPath,
        string reconciliationSummaryPath,
        string outputPath,
        CancellationToken cancellationToken = default);
}
