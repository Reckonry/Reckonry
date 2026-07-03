# Reckonry SDK

Status: Draft architecture

Reckonry will expose SDK contracts so third parties can build plugins without depending on internal implementation details.

This directory defines the intended SDK surface. It is architecture only. No SDK package is stable until the relevant contracts are promoted to a versioned NuGet package.

## SDK Areas

- [Importer SDK](importer-sdk.md)
- [Tax SDK](tax-sdk.md)
- [Report SDK](report-sdk.md)
- [Reconciliation SDK](reconciliation-sdk.md)

## Design Goals

- Keep `Reckonry.Core` independent of exchange, tax, report, and reconciliation implementations.
- Let plugins consume or produce canonical ledger data through explicit abstractions.
- Preserve source references and unknown data.
- Support dependency injection for automatic discovery.
- Expose metadata for compatibility, capabilities, and coverage.
- Version SDK contracts independently enough to support future external packages.

## Future NuGet Packages

Potential package split:

| Package | Purpose |
| --- | --- |
| `Reckonry.Core` | Canonical ledger models. |
| `Reckonry.Importers.Abstractions` | Importer SDK contracts. |
| `Reckonry.Tax.Abstractions` | Tax SDK contracts. |
| `Reckonry.Reports.Abstractions` | Report SDK contracts. |
| `Reckonry.Reconciliation.Abstractions` | Reconciliation SDK contracts. |
| `Reckonry.Pricing.Abstractions` | Pricing provider contracts. |
| `Reckonry.Plugin.Hosting` | Future plugin loading, validation, and dependency injection helpers. |

Concrete plugins should ship as separate packages, for example:

- `Reckonry.Importers.Binance`
- `Reckonry.Importers.Coinbase`
- `Reckonry.Tax.Italy`
- `Reckonry.Reports.Rw`

## Dependency Injection Model

Plugins should register services through extension methods:

```csharp
services.AddReckonryBinanceImporter();
services.AddReckonryItalyTaxModule();
services.AddReckonryRwReports();
services.AddReckonryBinanceReconciliation();
```

Host applications should discover plugins from injected collections:

- `IEnumerable<IExchangeImporter>`
- `IEnumerable<ITaxModule>`
- `IEnumerable<ILedgerReport>`
- `IEnumerable<IReconciliationProvider>`

Registries and factories should be built from these collections rather than hardcoded switch statements.

## Metadata Model

Every plugin SDK should expose descriptor metadata:

- Stable plugin id.
- Display name.
- Provider or jurisdiction.
- SDK contract version.
- Plugin implementation version.
- Supported inputs.
- Supported schemas.
- Supported operations.
- Compatibility range.
- Stability status.

Metadata exists so hosts can show capabilities, reject incompatible plugins, and produce reproducible audit output.

## Versioning

SDK contracts follow [Reckonry versioning](../versioning.md).

Before `1.0.0`, contracts may change with migration notes. After `1.0.0`, breaking SDK contract changes require a major version bump.

Plugins should declare:

- Plugin version.
- Required Reckonry SDK version range.
- Supported canonical ledger schema versions.
- Supported host capabilities.

## Non-Goals

- No plugin may invent financial data.
- No plugin may silently discard unknown data.
- No plugin may mutate existing ledger events.
- No plugin should depend on private application internals.
- No SDK should embed country-specific tax rules outside tax modules.
