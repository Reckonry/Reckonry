using Reckonry.Core;

namespace Reckonry.Importers.Abstractions;

public interface IExchangeImporter
{
    ImporterDescriptor Descriptor { get; }

    IReadOnlyList<LedgerEvent> ImportFolder(string inputFolder);
}
