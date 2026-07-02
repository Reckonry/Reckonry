using LedgerForge.Importers.Abstractions;

namespace LedgerForge.Importers.Coinbase;

public sealed class CoinbaseImporter : PlaceholderExchangeImporter
{
    public CoinbaseImporter()
        : base(new ImporterDescriptor
        {
            Id = "coinbase",
            DisplayName = "Coinbase Importer",
            Provider = "Coinbase",
            ImporterVersion = "0.0.0-placeholder",
            CoveragePercent = 0m,
            SupportedFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".csv" },
            SupportedFiles = ["Coinbase transaction CSV exports"],
            SupportedSchemas = ["Planned"],
            SupportedOperations = ["Planned"]
        })
    {
    }
}
