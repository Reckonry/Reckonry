using Reckonry.Importers.Abstractions;
using Reckonry.Pricing.Abstractions;
using Reckonry.Reconciliation.Abstractions;
using Reckonry.Reports;
using Reckonry.Tax.Abstractions;

namespace Reckonry.Plugins;

public sealed record PluginCatalog(
    IReadOnlyList<ISourceImporter> Importers,
    IReadOnlyList<ITaxModule> TaxModules,
    IReadOnlyList<IReportModule> Reports,
    IReadOnlyList<IReconciliationModule> ReconciliationModules,
    IReadOnlyList<IPriceProvider> PricingProviders);
