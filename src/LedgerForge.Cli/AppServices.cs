using LedgerForge.Audit;
using LedgerForge.Importers.Abstractions;
using LedgerForge.Importers.Bitstamp;
using LedgerForge.Importers.Binance;
using LedgerForge.Importers.Coinbase;
using LedgerForge.Importers.CryptoCom;
using LedgerForge.Importers.Kraken;
using LedgerForge.Importers.Revolut;
using LedgerForge.Reconciliation;
using LedgerForge.Reports;
using LedgerForge.Storage;
using LedgerForge.Tax.Abstractions;
using LedgerForge.Tax.Italy;
using LedgerForge.Tax.Italy.Rw;

internal sealed record AppServices(
    IImporterFactory ImporterFactory,
    ImporterRegistry ImporterRegistry,
    ILedgerReportWriter LedgerReportWriter,
    IRwSnapshotReportWriter RwSnapshotReportWriter,
    IRwValueReportWriter RwValueReportWriter,
    IBinanceReconciliationEngine BinanceReconciliationEngine,
    IIntegrityChecker IntegrityChecker,
    IItalyRwAccountantPackageWriter ItalyRwAccountantPackageWriter,
    IItalyRwConfigWorkflow ItalyRwConfigWorkflow,
    ILedgerStore LedgerStore,
    ILedgerValidator LedgerValidator,
    IReadOnlyList<IExchangeImporter> Importers,
    IReadOnlyList<ITaxModule> TaxModules)
{
    public static AppServices CreateDefault()
    {
        IExchangeImporter[] importers =
        [
            new BinanceCsvImporter(),
            new CoinbaseImporter(),
            new KrakenImporter(),
            new RevolutImporter(),
            new CryptoComImporter(),
            new BitstampImporter()
        ];
        var importerRegistry = new ImporterRegistry(importers);
        var importerFactory = new ImporterFactory(importerRegistry);
        var ledgerValidator = new JsonLedgerValidator();

        return new AppServices(
            importerFactory,
            importerRegistry,
            new LedgerReportWriter(),
            new RwSnapshotReportWriter(),
            new RwValueReportWriter(),
            new BinanceReconciliationEngine(),
            new IntegrityChecker(),
            new ItalyRwAccountantPackageWriter(),
            new ItalyRwConfigWorkflow(),
            new JsonLedgerStore(ledgerValidator),
            ledgerValidator,
            importers,
            new ITaxModule[] { new ItalyTaxModule() });
    }
}
