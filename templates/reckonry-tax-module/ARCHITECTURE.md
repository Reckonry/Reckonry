# Architecture Notes

Tax modules are country-specific plugins.

Responsibilities:

- Consume canonical ledger events without mutating them.
- Keep country-specific logic outside `Reckonry.Core`.
- Cite official sources in descriptors.
- Surface warnings and missing professional inputs.

Boundaries:

- Depends on `Reckonry.Core` and `Reckonry.Tax.Abstractions`.
- Does not depend on importers, provider modules, CLI, API, or generic reports.
- Does not claim legal or tax certainty.

This template intentionally performs no tax calculation.

