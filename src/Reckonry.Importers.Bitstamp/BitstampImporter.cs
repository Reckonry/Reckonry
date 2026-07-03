using Reckonry.Importers.Abstractions;

namespace Reckonry.Importers.Bitstamp;

public sealed class BitstampImporter : PlaceholderExchangeImporter
{
    public BitstampImporter()
        : base(new ImporterDescriptor
        {
            Id = "bitstamp",
            DisplayName = "Bitstamp Importer",
            Provider = "Bitstamp",
            ImporterVersion = "0.0.0-placeholder",
            CoveragePercent = 0m,
            SupportedFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".csv" },
            SupportedFiles = ["Bitstamp transaction CSV exports"],
            SupportedSchemas = ["Planned"],
            SupportedOperations = ["Planned"]
        })
    {
    }
}
