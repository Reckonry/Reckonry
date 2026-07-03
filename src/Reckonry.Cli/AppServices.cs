using Reckonry.Audit;
using Reckonry.Importers.Abstractions;
using Reckonry.Importers.Bitstamp;
using Reckonry.Importers.Binance;
using Reckonry.Importers.Coinbase;
using Reckonry.Importers.CryptoCom;
using Reckonry.Importers.Kraken;
using Reckonry.Importers.Revolut;
using Reckonry.Reconciliation;
using Reckonry.Reports;
using Reckonry.Storage;
using Reckonry.Tax.Abstractions;
using Reckonry.Tax.Italy;
using Reckonry.Tax.Italy.Rw;

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
    ITaxDossierPdfGenerator TaxDossierPdfGenerator,
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
            new TaxDossierPdfGenerator(),
            new JsonLedgerStore(ledgerValidator),
            ledgerValidator,
            importers,
            new ITaxModule[] { new ItalyTaxModule() });
    }
}
