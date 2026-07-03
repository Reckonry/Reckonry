using LedgerForge.Core;

namespace LedgerForge.Tax.Italy.Rw;

public interface IItalyRwAccountantPackageWriter
{
    Task<ItalyRwAccountantPackageResult> WriteAsync(
        string ledgerJsonPath,
        string outputFolder,
        int year,
        IReadOnlyCollection<LedgerEvent> ledgerEvents,
        string? language = null,
        CancellationToken cancellationToken = default);
}
