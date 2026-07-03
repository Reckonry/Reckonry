using Reckonry.Core;

namespace Reckonry.Reports;

public interface IRwValueReportWriter
{
    Task<IReadOnlyList<RwValueRow>> WriteAsync(
        string outputFolder,
        int year,
        IReadOnlyCollection<LedgerEvent> events,
        CancellationToken cancellationToken = default);
}
