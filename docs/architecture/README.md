# Architecture

This is the canonical Reckonry architecture overview.

Reckonry is organized around a canonical ledger model:

```text
Importers produce Ledger. Reports, reconciliation modules, and tax modules consume Ledger. Core depends on none of them.
```

```text
Source exports -> Importer modules -> Canonical ledger -> Reports / Reconciliation modules / Country tax modules
```

Importers read source exports and preserve source rows as `SourceReference` values. Unsupported or unrecognized rows must be represented as `LedgerEventType.Unknown` so they can be reviewed later instead of being silently discarded.

The canonical ledger belongs in `Reckonry.Core`. It models events, postings, source references, and decimal amounts without tax interpretation, country concepts, or provider concepts.

Reports consume the canonical ledger and produce reviewable files such as `ledger.json`, exception reports for unknown events, integrity reports, and scoped report artifacts.

Tax modules live outside Core so country-specific rules do not leak into the canonical transaction history.

## Projects

| Project | Responsibility |
| --- | --- |
| `Reckonry.Core` | Country-independent and provider-independent ledger domain model. |
| `Reckonry.Importers.Abstractions` | Source importer contracts, registry, and factory. |
| `Reckonry.Importers.Binance` | Binance provider implementation. |
| `Reckonry.Importers.Coinbase` | Coinbase provider implementation. |
| Other `Reckonry.Importers.*` placeholder projects | Planned provider modules; not supported parser implementations yet. |
| `Reckonry.Reports` | Generic report descriptors and ledger-derived generic reports. |
| `Reckonry.Reconciliation.Abstractions` | Generic reconciliation module contracts. |
| `Reckonry.Reconciliation.Binance.Italy` | Binance Italy provider/country reconciliation module. |
| `Reckonry.Reconciliation.Coinbase` | Coinbase provider-level reconciliation module for synthetic aggregate statement metadata. |
| `Reckonry.Tax.Abstractions` | Country tax module contracts and metadata records. |
| `Reckonry.Tax.Italy` | Italy-specific RW/professional-review artifacts; no final tax advice or capital gains logic. |
| `Reckonry.Plugins` | Bundled assembly discovery and plugin catalog helpers. |
| `Reckonry.Pricing.Abstractions` | Pricing provider contracts. |
| `Reckonry.Storage` | Ledger persistence abstractions and JSON storage. |
| `Reckonry.Cli` | CLI composition root and command-line adapter. |
| `Reckonry.Api` | Experimental in-memory host; not a supported public alpha API surface. |

## Dependency Direction

- `Reckonry.Core` has no dependencies on importers, tax modules, pricing, storage, reports, or reconciliation.
- Importers depend on `Reckonry.Core` and importer abstractions.
- Generic reports consume the ledger and storage abstractions.
- Country tax modules consume the ledger, tax abstractions, and report descriptors where needed.
- Reconciliation modules are read-only relative to the ledger.
- CLI and API hosts discover bundled Reckonry assemblies and build registries from descriptors.
- External IO is hidden behind interfaces where it crosses module boundaries.

## Public Alpha Model

The public alpha uses bundled assembly discovery:

```csharp
var catalog = PluginScanner.ScanPlugins();
```

This is not external binary plugin loading. External plugin loading, stable NuGet SDK packages, and compatibility ranges are planned work.

## Architecture Decisions

Durable decisions are tracked in [ADRs](../adr/README.md).

Historical architecture reviews are stored under `docs/reviews/`.
