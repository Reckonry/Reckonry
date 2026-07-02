using LedgerForge.Core;

namespace LedgerForge.Reports;

public interface ILedgerReportWriter
{
    Task WriteAsync(string ledgerJsonPath, IReadOnlyCollection<LedgerEvent> events, CancellationToken cancellationToken = default);
}
