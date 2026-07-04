# Architecture Notes

Reconciliation modules compare Reckonry outputs with external evidence.

Responsibilities:

- Be read-only.
- Preserve extraction status and uncertainty.
- Avoid printing private financial values in logs or chat-oriented output.
- Produce review artifacts that a professional can inspect.

Boundaries:

- Depends on `Reckonry.Reconciliation.Abstractions`.
- Does not depend on importers, tax modules, CLI, or API.
- Does not replace or edit canonical ledger data.

