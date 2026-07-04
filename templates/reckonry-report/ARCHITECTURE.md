# Architecture Notes

Report modules transform ledger data into review artifacts.

Responsibilities:

- Consume canonical ledger data without mutating it.
- Keep output deterministic.
- Explain counts and warnings.
- Avoid tax interpretation unless the report lives inside a tax module.

Boundaries:

- Depends on `Reckonry.Core` and `Reckonry.Reports`.
- Does not depend on importers, reconciliation modules, CLI, API, or tax modules.

