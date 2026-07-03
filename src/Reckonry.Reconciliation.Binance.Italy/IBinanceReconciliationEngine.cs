namespace Reckonry.Reconciliation.Binance.Italy;

public interface IBinanceReconciliationEngine
{
    Task<BinanceReconciliationSummary> ReconcileAsync(
        string officialReportsFolder,
        string reckonryReportsFolder,
        string outputFolder,
        CancellationToken cancellationToken = default);
}
