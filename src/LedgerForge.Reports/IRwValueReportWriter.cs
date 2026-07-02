using LedgerForge.Core;

namespace LedgerForge.Reports;

public interface IRwValueReportWriter
{
    Task<IReadOnlyList<RwValueRow>> WriteAsync(
        string outputFolder,
        int year,
        IReadOnlyCollection<LedgerEvent> events,
        CancellationToken cancellationToken = default);
}
