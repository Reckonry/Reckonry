# Contributing

Thanks for considering a contribution to LedgerForge.

## Ground Rules

- Keep tax interpretation and country-specific tax rules out of Core.
- Use `decimal` for financial and crypto amounts; never use `double`.
- Preserve source rows and raw source data in importer work.
- Unknown rows must be represented as `LedgerEventType.Unknown`, not discarded.
- Add or update tests for behavior changes.
- Keep changes small enough to review.

## Local Checks

```bash
dotnet format
dotnet build
dotnet test
```

## Licensing

By contributing, you agree that your contribution is provided under the repository license unless a separate written agreement applies.
