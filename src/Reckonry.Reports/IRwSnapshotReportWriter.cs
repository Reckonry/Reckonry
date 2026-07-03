using Reckonry.Core;

namespace Reckonry.Reports;

public interface IRwSnapshotReportWriter
{
    Task<IReadOnlyList<RwSnapshotRow>> WriteAsync(
        string outputFolder,
        int year,
        IReadOnlyCollection<LedgerEvent> events,
        CancellationToken cancellationToken = default);
}
