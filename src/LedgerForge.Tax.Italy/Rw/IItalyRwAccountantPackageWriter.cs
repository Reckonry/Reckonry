using LedgerForge.Core;

namespace LedgerForge.Tax.Italy.Rw;

public interface IItalyRwAccountantPackageWriter
{
    Task<ItalyRwAccountantPackageResult> WriteAsync(
        string ledgerJsonPath,
        string outputFolder,
        int year,
        IReadOnlyCollection<LedgerEvent> ledgerEvents,
        CancellationToken cancellationToken = default);
}
