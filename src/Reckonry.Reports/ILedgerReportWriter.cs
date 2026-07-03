using Reckonry.Core;

namespace Reckonry.Reports;

public interface ILedgerReportWriter
{
    Task WriteAsync(string ledgerJsonPath, IReadOnlyCollection<LedgerEvent> events, CancellationToken cancellationToken = default);
}
