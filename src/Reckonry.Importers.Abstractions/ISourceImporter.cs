using Reckonry.Core;

namespace Reckonry.Importers.Abstractions;

public interface ISourceImporter
{
    ImporterDescriptor Descriptor { get; }

    IReadOnlyList<LedgerEvent> ImportFolder(string inputFolder);
}
