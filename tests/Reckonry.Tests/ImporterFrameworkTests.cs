using Reckonry.Importers.Abstractions;
using Reckonry.Importers.Bitstamp;
using Reckonry.Importers.Binance;
using Reckonry.Importers.Coinbase;
using Reckonry.Importers.CryptoCom;
using Reckonry.Importers.Kraken;
using Reckonry.Importers.Revolut;

namespace Reckonry.Tests;

public sealed class ImporterFrameworkTests
{
    [Fact]
    public void Registry_DiscoversImportersFromInjectedCollection()
    {
        var registry = CreateRegistry();

        var descriptors = registry.ListDescriptors();

        Assert.Equal(6, descriptors.Count);
        Assert.Contains(descriptors, d => d.Id == "binance" && d.CoveragePercent > 0);
        Assert.Contains(descriptors, d => d.Id == "coinbase" && d.CoveragePercent > 0);
        Assert.Contains(descriptors, d => d.Id == "kraken" && d.CoveragePercent == 0);
        Assert.Contains(descriptors, d => d.Id == "revolut" && d.CoveragePercent == 0);
        Assert.Contains(descriptors, d => d.Id == "crypto.com" && d.CoveragePercent == 0);
        Assert.Contains(descriptors, d => d.Id == "bitstamp" && d.CoveragePercent == 0);
    }

    [Theory]
    [InlineData("binance", typeof(BinanceCsvImporter))]
    [InlineData("Binance", typeof(BinanceCsvImporter))]
    [InlineData("coinbase", typeof(CoinbaseImporter))]
    [InlineData("Coinbase", typeof(CoinbaseImporter))]
    [InlineData("crypto.com", typeof(CryptoComImporter))]
    [InlineData("cryptocom", typeof(CryptoComImporter))]
    public void Factory_CreatesImportersByIdOrExchangeName(string key, Type expectedType)
    {
        var factory = new ImporterFactory(CreateRegistry());

        var importer = factory.CreateRequired(key);

        Assert.IsType(expectedType, importer);
    }

    [Fact]
    public void PlaceholderImporters_ReturnClearNotSupportedMessage()
    {
        var inputFolder = Directory.CreateTempSubdirectory("reckonry-placeholder-importer-");
        try
        {
            var importer = new KrakenImporter();

            var ex = Assert.Throws<NotSupportedException>(() => importer.ImportFolder(inputFolder.FullName));

            Assert.Contains("plugin placeholder", ex.Message);
        }
        finally
        {
            inputFolder.Delete(recursive: true);
        }
    }

    private static ImporterRegistry CreateRegistry()
    {
        return new ImporterRegistry(
        [
            new BinanceCsvImporter(),
            new CoinbaseImporter(),
            new KrakenImporter(),
            new RevolutImporter(),
            new CryptoComImporter(),
            new BitstampImporter()
        ]);
    }
}
