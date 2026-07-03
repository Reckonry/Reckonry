namespace Reckonry.Reconciliation;

public interface IBinanceReconciliationEngine
{
    Task<BinanceReconciliationSummary> ReconcileAsync(
        string officialReportsFolder,
        string reckonryReportsFolder,
        string outputFolder,
        CancellationToken cancellationToken = default);
}
