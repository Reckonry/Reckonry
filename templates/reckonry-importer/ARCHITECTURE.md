# Architecture Notes

This importer is intentionally small.

Responsibilities:

- Parse one source format into canonical ledger events.
- Preserve every unsupported row as an unknown event.
- Avoid tax interpretation.
- Avoid provider-specific behavior outside the importer assembly.

Boundaries:

- Depends on `Reckonry.Core` and `Reckonry.Importers.Abstractions`.
- Does not depend on CLI, API, reports, reconciliation, or tax modules.
- Does not mutate existing ledger events.

Before expanding the importer, add tests for every new row shape and every
unknown-row fallback.

