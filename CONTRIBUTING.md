# Contributing

Thanks for considering a contribution to Reckonry.

Reckonry is open-source infrastructure for verifiable financial ledgers. Contributions are welcome when they preserve auditability, privacy, and clear evidence trails.

## Ground Rules

- Keep tax interpretation and country-specific tax rules out of `Reckonry.Core`.
- Use `decimal` for financial and digital asset amounts; never use `double`.
- Preserve source rows and raw source data in importer work.
- Unknown rows must be represented as `LedgerEventType.Unknown`, not discarded.
- Do not invent financial values in tests, docs, samples, or generated artifacts.
- Do not commit real financial data, real exchange exports, account identifiers, private tax configuration, or unredacted logs.
- Add or update tests for behavior changes.
- Keep changes small enough to review.

## Development Flow

1. Open or reference an issue for non-trivial changes.
2. Keep product, ledger, importer, report, and tax boundaries clear.
3. Update documentation for user-visible behavior.
4. Update ADRs when architecture changes.
5. Run local checks before opening a pull request.

## Local Checks

```bash
dotnet build Reckonry.sln
dotnet test Reckonry.sln
```

`dotnet format` is also recommended before larger pull requests.

## Tax And Regulatory Changes

Country-specific tax behavior requires official sources. Informal summaries, blog posts, social media posts, or unsupported assumptions are not enough for implementation.

Tax modules may interpret the canonical ledger, but they must not modify it.

## Privacy

All fixtures, examples, screenshots, logs, and sample files must be fake, synthetic, or anonymized. When in doubt, do not include the data.

## Licensing

By contributing, you agree that your contribution is provided under the repository license unless a separate written agreement applies.
