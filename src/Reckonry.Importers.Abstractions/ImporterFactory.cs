namespace Reckonry.Importers.Abstractions;

public sealed class ImporterFactory : IImporterFactory
{
    private readonly ImporterRegistry registry;

    public ImporterFactory(ImporterRegistry registry)
    {
        this.registry = registry;
    }

    public IReadOnlyList<ImporterDescriptor> ListImporters()
    {
        return registry.ListDescriptors();
    }

    public bool TryCreate(string importerIdOrSource, out ISourceImporter importer)
    {
        return registry.TryGet(importerIdOrSource, out importer);
    }

    public ISourceImporter CreateRequired(string importerIdOrSource)
    {
        if (TryCreate(importerIdOrSource, out var importer))
        {
            return importer;
        }

        throw new KeyNotFoundException($"No source importer is registered for '{importerIdOrSource}'.");
    }
}
