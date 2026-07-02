namespace LedgerForge.Importers.Abstractions;

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

    public bool TryCreate(string importerIdOrExchange, out IExchangeImporter importer)
    {
        return registry.TryGet(importerIdOrExchange, out importer);
    }

    public IExchangeImporter CreateRequired(string importerIdOrExchange)
    {
        if (TryCreate(importerIdOrExchange, out var importer))
        {
            return importer;
        }

        throw new KeyNotFoundException($"No exchange importer is registered for '{importerIdOrExchange}'.");
    }
}
