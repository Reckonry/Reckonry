using Reckonry.Audit;
using Reckonry.Importers.Abstractions;
using Reckonry.Plugins;
using Reckonry.Reconciliation.Abstractions;
using Reckonry.Reports;
using Reckonry.Storage;
using Reckonry.Tax.Abstractions;
using Reckonry.Tax.Italy.Rw;

internal sealed record AppServices(
    IImporterFactory ImporterFactory,
    ImporterRegistry ImporterRegistry,
    ILedgerReportWriter LedgerReportWriter,
    IRwSnapshotReportWriter RwSnapshotReportWriter,
    IRwValueReportWriter RwValueReportWriter,
    IIntegrityChecker IntegrityChecker,
    IItalyRwAccountantPackageWriter ItalyRwAccountantPackageWriter,
    IItalyRwConfigWorkflow ItalyRwConfigWorkflow,
    ITaxDossierPdfGenerator TaxDossierPdfGenerator,
    ILedgerStore LedgerStore,
    ILedgerValidator LedgerValidator,
    PluginCatalog Plugins)
{
    public IReadOnlyList<ISourceImporter> Importers => Plugins.Importers;

    public IReadOnlyList<ITaxModule> TaxModules => Plugins.TaxModules;

    public IReadOnlyList<IReportModule> Reports => Plugins.Reports;

    public IReadOnlyList<IReconciliationModule> ReconciliationModules => Plugins.ReconciliationModules;

    public static AppServices CreateDefault()
    {
        var plugins = PluginScanner.ScanPlugins();
        var importerRegistry = new ImporterRegistry(plugins.Importers);
        var importerFactory = new ImporterFactory(importerRegistry);
        var ledgerValidator = new JsonLedgerValidator();

        return new AppServices(
            importerFactory,
            importerRegistry,
            Resolve<ILedgerReportWriter>(new LedgerReportWriter()),
            Resolve<IRwSnapshotReportWriter>(plugins),
            Resolve<IRwValueReportWriter>(plugins),
            new IntegrityChecker(),
            Resolve<IItalyRwAccountantPackageWriter>(plugins),
            Resolve<IItalyRwConfigWorkflow>(new ItalyRwConfigWorkflow()),
            Resolve<ITaxDossierPdfGenerator>(plugins),
            new JsonLedgerStore(ledgerValidator),
            ledgerValidator,
            plugins);
    }

    public bool TryGetReconciliationModule(
        string providerId,
        string countryCode,
        out IReconciliationModule module)
    {
        module = ReconciliationModules.FirstOrDefault(candidate =>
            string.Equals(candidate.Descriptor.ProviderId, providerId, StringComparison.OrdinalIgnoreCase)
            && (string.Equals(candidate.Descriptor.CountryCode, countryCode, StringComparison.OrdinalIgnoreCase)
                || (candidate.Descriptor.CountryCode is null && IsGenericReconciliationCountry(countryCode))))!;

        return module is not null;
    }

    private static bool IsGenericReconciliationCountry(string countryCode)
    {
        return countryCode is "GLOBAL" or "GENERIC" or "ANY";
    }

    private static T Resolve<T>(PluginCatalog plugins)
    {
        var candidate = plugins.Importers.OfType<T>()
            .Concat(plugins.TaxModules.OfType<T>())
            .Concat(plugins.Reports.OfType<T>())
            .Concat(plugins.ReconciliationModules.OfType<T>())
            .Concat(plugins.PricingProviders.OfType<T>())
            .FirstOrDefault();

        return candidate ?? throw new InvalidOperationException($"Required plugin was not discovered: {typeof(T).FullName}");
    }

    private static T Resolve<T>(T fallback)
        where T : notnull
    {
        return fallback;
    }
}
