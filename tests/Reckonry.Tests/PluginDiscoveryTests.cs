using Reckonry.Plugins;

namespace Reckonry.Tests;

public sealed class PluginDiscoveryTests
{
    [Fact]
    public void ScanPlugins_DiscoversInstalledPlatformPlugins()
    {
        var plugins = PluginScanner.ScanPlugins();

        Assert.Contains(plugins.Importers, importer => importer.Descriptor.Id == "binance");
        Assert.Contains(plugins.TaxModules, module => module.Descriptor.CountryCode == "IT");
        Assert.Contains(plugins.Reports, report => report.Descriptor.Id == "ledger" && report.Descriptor.CountryCode is null);
        Assert.Contains(plugins.Reports, report => report.Descriptor.Id == "italy-rw-snapshot" && report.Descriptor.CountryCode == "IT");
        Assert.Contains(plugins.ReconciliationModules, module =>
            module.Descriptor.ProviderId == "binance"
            && module.Descriptor.CountryCode == "IT");
    }
}
