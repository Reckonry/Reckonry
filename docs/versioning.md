# Versioning Strategy

LedgerForge uses Semantic Versioning.

Version format:

```text
MAJOR.MINOR.PATCH
```

- `MAJOR`: incompatible public changes.
- `MINOR`: backward-compatible feature additions.
- `PATCH`: backward-compatible fixes.

LedgerForge is early-stage software. Until `1.0.0`, APIs, CLI commands, schemas, and project boundaries may still change, but breaking changes must still be documented with migration notes.

## Version Lines

## 0.x

The `0.x` line is early alpha.

Expected behavior:

- Public contracts may change.
- Canonical ledger schema may evolve.
- CLI commands may be renamed or reorganized.
- Importer coverage may change as unsupported formats become supported.
- Breaking changes require migration notes.
- Real financial data must never be committed while testing migrations.

Compatibility target:

- Preserve source data whenever possible.
- Keep old generated ledgers readable where practical.
- Prefer additive changes, even before `1.0.0`.

## 1.x

The `1.x` line is the first stable LedgerForge release line.

Expected behavior:

- Canonical ledger v1 is stable.
- CLI commands documented in the README are stable.
- Importer plugin contracts are stable for the `1.x` line.
- Reports remain reproducible from the canonical ledger.
- Breaking changes are avoided.

Compatibility target:

- Backward-compatible additions are allowed in minor releases.
- Patch releases must not change public behavior except to fix defects.
- Existing valid `1.x` ledgers should continue to validate and load.

## 2.x

The `2.x` line is reserved for intentional breaking changes.

Examples:

- New canonical ledger schema with incompatible semantics.
- CLI command removals or incompatible option changes.
- Importer plugin contract changes that require plugin updates.
- Report format changes that require migration.

Compatibility target:

- Provide migration notes.
- Provide schema transition guidance.
- Keep read-only migration tooling where practical.

## Schema Evolution

The canonical ledger schema has its own schema version, such as:

```text
ledgerforge-ledger-v1
```

Rules:

- Additive metadata fields are allowed without changing the schema version.
- Breaking schema changes require a new schema version.
- Unknown source data must remain preserved across schema versions.
- Schema migrations must not invent financial data.
- Schema migrations must document how source references and unknown events are preserved.

## JSON Compatibility

Ledger JSON compatibility follows these rules:

- JSON object root is required for canonical ledgers.
- `schemaVersion` identifies the canonical format.
- Readers may support older formats for migration.
- Validators should validate the declared canonical version.
- Consumers must ignore unknown additive metadata fields.
- Existing field semantics must not change inside a stable schema version.
- Decimal-compatible JSON numbers are required for financial and crypto amounts.

## Importer Compatibility

Importer compatibility covers the relationship between source exports and canonical ledger events.

Rules:

- Importers produce canonical ledger events.
- Importers must preserve source references and raw data.
- New importer coverage should reduce unknown rows, not discard them.
- Changes to event classification, posting direction, amount mapping, or source preservation are compatibility-sensitive.
- Importer behavior changes must include tests and changelog notes.
- If importer output semantics change, migration notes must explain how to regenerate affected ledgers.

## CLI Compatibility

CLI compatibility covers documented commands, options, exit codes, and output contracts.

Rules:

- New commands and options may be added in minor releases.
- Existing documented commands should remain stable during a major version line.
- Breaking CLI changes require a major version bump after `1.0.0`.
- Validation commands must keep machine-readable success/failure behavior stable where documented.
- Human-readable output may improve, but scripts should not be broken without migration notes.

## Future Plugin Compatibility

LedgerForge is designed for importer, pricing, tax, report, audit, and reconciliation plugins.

Future plugin compatibility rules:

- Plugin contracts must be versioned.
- Plugin descriptors must declare plugin identity, version, supported files, schemas, operations, and coverage where applicable.
- Plugins must consume or produce canonical ledger data through documented abstractions.
- Plugins must not mutate existing ledger events.
- Breaking plugin contract changes require a major version bump once plugin contracts are stable.
- Host applications should be able to reject incompatible plugins with clear errors.

## Changelog And Migration Notes

Every user-visible change should update the changelog.

Breaking changes require migration notes that explain:

- What changed.
- Who is affected.
- How to detect affected files or workflows.
- How to migrate or regenerate outputs.
- Whether older formats remain readable.
