# Architecture

Reckonry is organized around a canonical ledger model. The core rule is:

```text
Importers produce Ledger. Reports, reconciliation modules, and tax modules consume Ledger. Core depends on none of them.
```

```text
Source exports -> Importer modules -> Canonical ledger -> Reports / Reconciliation modules / Country tax modules
```

Importers read source exports and preserve source rows as `SourceReference` values. Unsupported or unrecognized rows must be represented as `LedgerEventType.Unknown` so they can be reviewed later instead of being silently discarded.

The canonical ledger belongs in `Reckonry.Core`. It models events, postings, source references, and decimal amounts without tax interpretation.

Reports consume the canonical ledger and produce reviewable files such as `ledger.json`, exception reports for unknown events, integrity reports, and country-scoped report artifacts.

Tax modules live outside Core so country-specific rules do not leak into the canonical transaction history.

## Projects

| Project | Responsibility |
| --- | --- |
| `Reckonry.Core` | Country-independent and exchange-independent ledger domain model. |
| `Reckonry.Importers.Abstractions` | Source importer contracts. |
| `Reckonry.Importers.Binance` | Binance provider implementation. |
| `Reckonry.Reports` | Generic report descriptors and ledger-derived generic reports. |
| `Reckonry.Reconciliation.Abstractions` | Generic reconciliation module contracts. |
| `Reckonry.Reconciliation.Binance.Italy` | Binance Italy provider/country reconciliation module. |
| `Reckonry.Tax.Abstractions` | Country module contracts. |
| `Reckonry.Tax.Italy` | Italy-specific draft tax/report models, including RW report artifacts; no final tax advice or capital gains logic. |
| `Reckonry.Plugins` | Bundled assembly discovery and plugin catalog helpers. |
| `Reckonry.Pricing.Abstractions` | Pricing provider contracts. |
| `Reckonry.Storage` | Ledger persistence abstractions and JSON storage. |
| `Reckonry.Cli` | Composition root and command-line adapter. |

## Dependency Direction

- `Core` has no dependencies on importers, tax modules, pricing, storage, reports, or reconciliation.
- Importers depend on `Core` and importer abstractions.
- Generic reports consume `Core` and storage.
- Country tax modules consume `Core`, tax abstractions, and report descriptors where needed.
- The CLI and API are host applications that discover bundled Reckonry assemblies and build registries from descriptors.
- External IO is hidden behind interfaces where it crosses module boundaries.
