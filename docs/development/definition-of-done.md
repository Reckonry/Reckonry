# Definition of Done

This document defines what "Done" means for Reckonry.

A feature, fix, report, importer, audit check, reconciliation change, tax module, pricing module, storage change, or CLI command is not done until every applicable item below is satisfied.

## Required Checks

- Unit tests added or updated.
- Integration tests added or updated where behavior crosses project boundaries, file I/O, CLI commands, importers, reports, storage, audit, reconciliation, or external formats.
- No compiler warnings.
- `dotnet build` completes cleanly.
- `dotnet test` completes cleanly.
- No TODO comments left in the changed feature area unless they are intentional, documented, and linked to follow-up work.
- Documentation updated.
- ADR updated when the change affects architecture, boundaries, format contracts, persistence, plugin behavior, tax module behavior, audit/reconciliation behavior, or source-of-truth rules.
- README updated when behavior is user-visible.
- Changelog updated for user-visible behavior, format changes, CLI changes, importer coverage changes, report changes, or breaking changes.
- Privacy reviewed.
- No breaking changes without migration notes.
- No real financial data committed.

## Privacy Review

Every change must preserve the Reckonry privacy posture:

- Real exchange exports stay under ignored local folders such as `input/`.
- Generated private ledgers, reports, audit outputs, and reconciliation outputs stay under ignored local folders such as `output/`.
- Tests, samples, docs, logs, screenshots, and commits must use fake or anonymized data only.
- No private balances, quantities, addresses, transaction IDs, raw rows, or financial values are copied into repository files.
- Unknown source rows remain preserved in generated local outputs, but private raw rows must never be committed.

## Architecture Review

Use ADRs when a change alters durable project decisions, including:

- Canonical ledger format.
- Importer plugin contracts.
- Storage and validation behavior.
- Report immutability.
- Audit or reconciliation responsibilities.
- Tax module boundaries.
- Pricing provider boundaries.
- Source-of-truth rules.
- Migration or compatibility rules.

## Breaking Changes

Breaking changes require migration notes before the work is considered done.

Examples:

- Canonical ledger schema changes.
- CLI command or option changes.
- Report file format changes.
- Importer output semantics changes.
- Public interface changes across projects.
- Changes that invalidate existing generated ledgers or reports.

Migration notes must explain:

- What changed.
- Who is affected.
- How to detect affected files or workflows.
- How to migrate or regenerate outputs.
- Whether old formats remain readable.

## PR Checklist

Copy this checklist into pull requests:

```markdown
## Definition of Done

- [ ] Unit tests added or updated.
- [ ] Integration tests added or updated where applicable.
- [ ] No compiler warnings.
- [ ] `dotnet build` completes cleanly.
- [ ] `dotnet test` completes cleanly.
- [ ] No TODO comments left in the changed feature area unless documented.
- [ ] Documentation updated.
- [ ] ADR updated if architecture changed.
- [ ] README updated if user-visible behavior changed.
- [ ] Changelog updated where applicable.
- [ ] Privacy reviewed.
- [ ] No breaking changes, or migration notes are included.
- [ ] No real financial data committed.
```
