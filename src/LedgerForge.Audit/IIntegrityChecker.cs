using LedgerForge.Core;

namespace LedgerForge.Audit;

public interface IIntegrityChecker
{
    LedgerIntegrityReport Check(IReadOnlyCollection<LedgerEvent> events);

    Task<LedgerIntegrityReport> WriteAsync(
        string outputFolder,
        IReadOnlyCollection<LedgerEvent> events,
        CancellationToken cancellationToken = default);
}
