# Architecture

LedgerForge uses a clean architecture boundary around a canonical ledger model:

```text
CSV exports -> importer -> canonical ledger -> reports -> future tax modules
```

Importers read exchange exports and preserve source rows as `SourceReference` values. Unsupported or unrecognized rows must be represented as `LedgerEventType.Unknown` so they can be reviewed later instead of being silently discarded.

The canonical ledger belongs in `LedgerForge.Core`. It models events, postings, source references, and decimal amounts without tax interpretation.

Reports consume the canonical ledger and produce auditable files such as `ledger.json` and exception reports for unknown events.

Future tax modules should be built outside Core so country-specific rules do not leak into the canonical transaction history.
