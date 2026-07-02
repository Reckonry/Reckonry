using LedgerForge.Core;

namespace LedgerForge.Storage;

public interface ILedgerStore
{
    Task<IReadOnlyList<LedgerEvent>> ReadAsync(
        string ledgerJsonPath,
        CancellationToken cancellationToken = default);

    Task WriteAsync(
        string ledgerJsonPath,
        IReadOnlyCollection<LedgerEvent> events,
        CancellationToken cancellationToken = default);
}
