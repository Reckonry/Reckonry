namespace Reckonry.Reconciliation.Abstractions;

public interface IReconciliationModule
{
    ReconciliationModuleDescriptor Descriptor { get; }

    Task<ReconciliationRunResult> ReconcileAsync(
        ReconciliationRunRequest request,
        CancellationToken cancellationToken = default);
}
