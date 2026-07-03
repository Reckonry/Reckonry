namespace Reckonry.Importers.Abstractions;

public interface IImporterFactory
{
    IReadOnlyList<ImporterDescriptor> ListImporters();

    bool TryCreate(string importerIdOrExchange, out IExchangeImporter importer);

    IExchangeImporter CreateRequired(string importerIdOrExchange);
}
