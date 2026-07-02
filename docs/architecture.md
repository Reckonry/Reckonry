# Architecture

LedgerForge uses Clean Architecture around a canonical ledger model. The core rule is:

```text
Importers produce Ledger. Tax modules consume Ledger. Core depends on neither.
```

```text
Exchange exports -> Importer plugins -> Canonical ledger -> Reports / Reconciliation / Tax modules
```

Importers read exchange exports and preserve source rows as `SourceReference` values. Unsupported or unrecognized rows must be represented as `LedgerEventType.Unknown` so they can be reviewed later instead of being silently discarded.

The canonical ledger belongs in `LedgerForge.Core`. It models events, postings, source references, and decimal amounts without tax interpretation.

Reports consume the canonical ledger and produce auditable files such as `ledger.json` and exception reports for unknown events.

Tax modules live outside Core so country-specific rules do not leak into the canonical transaction history.

## Projects

| Project | Responsibility |
| --- | --- |
| `LedgerForge.Core` | Country-independent and exchange-independent ledger domain model. |
| `LedgerForge.Importers.Abstractions` | Plugin-ready importer contracts. |
| `LedgerForge.Importers.Binance` | Binance provider implementation. |
| `LedgerForge.Reports` | Ledger-derived reports such as RW snapshots and value summaries. |
| `LedgerForge.Reconciliation` | Validation against official exchange-issued reports. |
| `LedgerForge.Tax.Abstractions` | Country module contracts. |
| `LedgerForge.Tax.Italy` | Italy module placeholder; no tax advice or capital gains logic yet. |
| `LedgerForge.Pricing.Abstractions` | Pricing provider contracts. |
| `LedgerForge.Storage` | Ledger persistence abstractions and JSON storage. |
| `LedgerForge.Cli` | Composition root and command-line adapter. |

## Dependency Direction

- `Core` has no dependencies on importers, tax modules, pricing, storage, reports, or reconciliation.
- Importers depend on `Core` and importer abstractions.
- Reports and tax modules consume `Core`.
- The CLI is the composition root and wires concrete plugins through interfaces.
- External IO is hidden behind interfaces where it crosses module boundaries.
