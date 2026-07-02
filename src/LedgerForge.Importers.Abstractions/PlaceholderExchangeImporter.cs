using LedgerForge.Core;

namespace LedgerForge.Importers.Abstractions;

public abstract class PlaceholderExchangeImporter : IExchangeImporter
{
    protected PlaceholderExchangeImporter(ImporterDescriptor descriptor)
    {
        Descriptor = descriptor;
    }

    public ImporterDescriptor Descriptor { get; }

    public IReadOnlyList<LedgerEvent> ImportFolder(string inputFolder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputFolder);

        if (!Directory.Exists(inputFolder))
        {
            throw new DirectoryNotFoundException($"Input folder was not found: {inputFolder}");
        }

        throw new NotSupportedException(
            $"{Descriptor.DisplayName} is registered as a plugin placeholder. Parser implementation is planned.");
    }
}
