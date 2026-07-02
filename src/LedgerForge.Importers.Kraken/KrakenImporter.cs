using LedgerForge.Importers.Abstractions;

namespace LedgerForge.Importers.Kraken;

public sealed class KrakenImporter : PlaceholderExchangeImporter
{
    public KrakenImporter()
        : base(new ImporterDescriptor
        {
            Id = "kraken",
            DisplayName = "Kraken Importer",
            Provider = "Kraken",
            ImporterVersion = "0.0.0-placeholder",
            CoveragePercent = 0m,
            SupportedFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".csv" },
            SupportedFiles = ["Kraken ledger and trade CSV exports"],
            SupportedSchemas = ["Planned"],
            SupportedOperations = ["Planned"]
        })
    {
    }
}
