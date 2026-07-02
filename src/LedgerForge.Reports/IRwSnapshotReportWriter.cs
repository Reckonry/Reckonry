using LedgerForge.Core;

namespace LedgerForge.Reports;

public interface IRwSnapshotReportWriter
{
    Task<IReadOnlyList<RwSnapshotRow>> WriteAsync(
        string outputFolder,
        int year,
        IReadOnlyCollection<LedgerEvent> events,
        CancellationToken cancellationToken = default);
}
