# Reckonry SDK

Status: Current internal contracts, pre-stable.

Reckonry exposes internal contracts for bundled modules. These contracts are
implemented in source today, but they are not stable external NuGet SDKs yet.

This directory documents the current contract surface. Any future API or package
idea is marked `Planned`.

## SDK Areas

- [Importer SDK](importer-sdk.md)
- [Tax SDK](tax-sdk.md)
- [Report SDK](report-sdk.md)
- [Reconciliation SDK](reconciliation-sdk.md)
- [Plugin Templates](plugin-template.md)

Pricing abstractions are implemented in `Reckonry.Pricing.Abstractions`, but
there is no dedicated pricing SDK guide yet. The implemented contract is
documented below until pricing becomes a stable external SDK surface.

## Implemented Packages

| Package | Implemented responsibility |
| --- | --- |
| `Reckonry.Core` | Canonical ledger models. |
| `Reckonry.Importers.Abstractions` | Source importer contracts, registry, and factory. |
| `Reckonry.Tax.Abstractions` | Country tax module contracts and metadata records. |
| `Reckonry.Reports` | Report descriptors, generic report modules, and ledger writer interface. |
| `Reckonry.Reconciliation.Abstractions` | Reconciliation module contracts and run records. |
| `Reckonry.Pricing.Abstractions` | Price provider contract and quote records. |
| `Reckonry.Plugins` | Bundled assembly discovery and plugin catalog helpers. |

## Implemented Discovery Model

Hosts discover bundled modules from Reckonry assemblies:

```csharp
var plugins = PluginScanner.ScanPlugins();
```

`PluginCatalog` matches the source code exactly:

```csharp
public sealed record PluginCatalog(
    IReadOnlyList<ISourceImporter> Importers,
    IReadOnlyList<ITaxModule> TaxModules,
    IReadOnlyList<IReportModule> Reports,
    IReadOnlyList<IReconciliationModule> ReconciliationModules,
    IReadOnlyList<IPriceProvider> PricingProviders);
```

`PluginScanner.ScanPlugins` matches the source code exactly:

```csharp
public static PluginCatalog ScanPlugins(params Assembly[] assemblies)
```

If no assemblies are supplied, the scanner loads `Reckonry.*.dll` assemblies
from the host output directory and combines them with already loaded Reckonry
assemblies. If assemblies are supplied, it scans those assemblies only.

The scanner creates non-abstract, non-interface types assignable to each SDK
interface. Constructors must have only optional parameters or no parameters.

This is bundled assembly discovery. It is not external binary plugin loading.

## Implemented Pricing Abstractions

These pricing contracts are implemented, but no concrete pricing provider is
installed today.

```csharp
public interface IPriceProvider
{
    string ProviderId { get; }

    Task<PriceQuoteResult> GetQuoteAsync(
        PriceQuoteRequest request,
        CancellationToken cancellationToken = default);
}
```

```csharp
public sealed record PriceQuoteRequest(
    string AssetSymbol,
    string CurrencyCode,
    DateTimeOffset TimestampUtc);
```

```csharp
public sealed record PriceQuoteResult(
    PriceQuoteRequest Request,
    decimal? Price,
    string Source,
    string? Warning);
```

## Planned

The following items are planned and are not stable implemented external SDKs
today:

- external NuGet packages for importer, tax, report, reconciliation, and pricing
  extension authors
- external binary plugin loading
- explicit SDK contract versions on every descriptor
- compatibility ranges on every descriptor
- stable package split for third-party modules

Future external packages may include:

- `Reckonry.Importers.Binance`
- `Reckonry.Importers.Coinbase`
- `Reckonry.Tax.Italy`
- `Reckonry.Reconciliation.Binance.Italy`

Source-based `dotnet new` templates are available today under
[`templates/`](../../templates/). See [Plugin Templates](plugin-template.md).

## Versioning

SDK contracts follow [Reckonry versioning](../../VERSIONING.md).

Before `1.0.0`, contracts may change with migration notes. After `1.0.0`,
breaking SDK contract changes require a major version bump.

## Non-Goals

- No module may invent financial data.
- No module may silently discard unknown data.
- No module may mutate existing ledger events.
- No module should depend on private application internals.
- No SDK should embed country-specific tax rules outside tax modules.
