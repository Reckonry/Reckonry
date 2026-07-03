# Architecture

Reckonry uses Clean Architecture around a canonical ledger model. The core rule is:

```text
Importers produce Ledger. Tax modules consume Ledger. Core depends on neither.
```

```text
Exchange exports -> Importer plugins -> Canonical ledger -> Reports / Reconciliation / Tax modules
```

Importers read exchange exports and preserve source rows as `SourceReference` values. Unsupported or unrecognized rows must be represented as `LedgerEventType.Unknown` so they can be reviewed later instead of being silently discarded.

The canonical ledger belongs in `Reckonry.Core`. It models events, postings, source references, and decimal amounts without tax interpretation.

Reports consume the canonical ledger and produce auditable files such as `ledger.json` and exception reports for unknown events.

Tax modules live outside Core so country-specific rules do not leak into the canonical transaction history.

## Projects

| Project | Responsibility |
| --- | --- |
| `Reckonry.Core` | Country-independent and exchange-independent ledger domain model. |
| `Reckonry.Importers.Abstractions` | Plugin-ready importer contracts. |
| `Reckonry.Importers.Binance` | Binance provider implementation. |
| `Reckonry.Reports` | Ledger-derived reports such as RW snapshots and value summaries. |
| `Reckonry.Reconciliation` | Validation against official exchange-issued reports. |
| `Reckonry.Tax.Abstractions` | Country module contracts. |
| `Reckonry.Tax.Italy` | Italy-specific draft tax/report models, including the official RW crypto model; no final tax advice or capital gains logic. |
| `Reckonry.Pricing.Abstractions` | Pricing provider contracts. |
| `Reckonry.Storage` | Ledger persistence abstractions and JSON storage. |
| `Reckonry.Cli` | Composition root and command-line adapter. |

## Dependency Direction

- `Core` has no dependencies on importers, tax modules, pricing, storage, reports, or reconciliation.
- Importers depend on `Core` and importer abstractions.
- Reports and tax modules consume `Core`.
- The CLI is the composition root and wires concrete plugins through interfaces.
- External IO is hidden behind interfaces where it crosses module boundaries.
