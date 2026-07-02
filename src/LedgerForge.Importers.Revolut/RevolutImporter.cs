using LedgerForge.Importers.Abstractions;

namespace LedgerForge.Importers.Revolut;

public sealed class RevolutImporter : PlaceholderExchangeImporter
{
    public RevolutImporter()
        : base(new ImporterDescriptor
        {
            Id = "revolut",
            DisplayName = "Revolut Importer",
            Provider = "Revolut",
            ImporterVersion = "0.0.0-placeholder",
            CoveragePercent = 0m,
            SupportedFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".csv" },
            SupportedFiles = ["Revolut crypto account CSV exports"],
            SupportedSchemas = ["Planned"],
            SupportedOperations = ["Planned"]
        })
    {
    }
}
