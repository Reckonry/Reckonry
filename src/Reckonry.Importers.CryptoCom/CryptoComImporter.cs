using Reckonry.Importers.Abstractions;

namespace Reckonry.Importers.CryptoCom;

public sealed class CryptoComImporter : PlaceholderExchangeImporter
{
    public CryptoComImporter()
        : base(new ImporterDescriptor
        {
            Id = "crypto.com",
            DisplayName = "Crypto.com Importer",
            Provider = "Crypto.com",
            ImporterVersion = "0.0.0-placeholder",
            CoveragePercent = 0m,
            SupportedFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".csv" },
            SupportedFiles = ["Crypto.com app and exchange CSV exports"],
            SupportedSchemas = ["Planned"],
            SupportedOperations = ["Planned"]
        })
    {
    }
}
