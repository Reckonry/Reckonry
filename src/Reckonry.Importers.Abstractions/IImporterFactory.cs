namespace Reckonry.Importers.Abstractions;

public interface IImporterFactory
{
    IReadOnlyList<ImporterDescriptor> ListImporters();

    bool TryCreate(string importerIdOrSource, out ISourceImporter importer);

    ISourceImporter CreateRequired(string importerIdOrSource);
}
