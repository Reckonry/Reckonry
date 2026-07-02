using LedgerForge.Core;

namespace LedgerForge.Importers.Abstractions;

public interface IExchangeImporter
{
    ImporterDescriptor Descriptor { get; }

    IReadOnlyList<LedgerEvent> ImportFolder(string inputFolder);
}
