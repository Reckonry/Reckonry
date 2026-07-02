namespace LedgerForge.Reconciliation;

public interface IBinanceReconciliationEngine
{
    Task<BinanceReconciliationSummary> ReconcileAsync(
        string officialReportsFolder,
        string ledgerForgeReportsFolder,
        string outputFolder,
        CancellationToken cancellationToken = default);
}
