using Reckonry.Core;

namespace Reckonry.Audit;

public interface IIntegrityChecker
{
    LedgerIntegrityReport Check(IReadOnlyCollection<LedgerEvent> events);

    Task<LedgerIntegrityReport> WriteAsync(
        string outputFolder,
        IReadOnlyCollection<LedgerEvent> events,
        CancellationToken cancellationToken = default);
}
