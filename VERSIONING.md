# Versioning

This is the canonical Reckonry versioning policy.

Reckonry uses semantic versioning for application releases and explicit compatibility expectations for the CLI, canonical ledger schema, importers, SDK contracts, reports, reconciliation modules, and tax modules.

## Version Format

Application releases use:

```text
MAJOR.MINOR.PATCH
```

Prerelease labels may be appended:

```text
0.1.0-alpha
0.2.0-beta.1
1.0.0-rc.1
```

- `MAJOR`: incompatible public changes.
- `MINOR`: backward-compatible feature additions.
- `PATCH`: backward-compatible fixes.

Reckonry is early-stage software. Until `1.0.0`, APIs, CLI commands, schemas, and project boundaries may still change, but breaking changes must be documented with migration notes.

## Version Lines

### 0.x

The `0.x` line is early alpha/beta software.

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

### 1.x

The `1.x` line is the first stable Reckonry release line.

Expected behavior:

- Canonical ledger v1 is stable.
- CLI commands documented in the README and release docs are stable.
- Importer and SDK contracts are stable for the `1.x` line.
- Reports remain reproducible from the canonical ledger.
- Breaking changes are avoided.

Compatibility target:

- Backward-compatible additions are allowed in minor releases.
- Patch releases must not change public behavior except to fix defects.
- Existing valid `1.x` ledgers should continue to validate and load.

### 2.x

The `2.x` line is reserved for intentional breaking changes.

Examples:

- New canonical ledger schema with incompatible semantics.
- CLI command removals or incompatible option changes.
- Importer or SDK contract changes that require module updates.
- Report format changes that require migration.

Compatibility target:

- Provide migration notes.
- Provide schema transition guidance.
- Keep read-only migration tooling where practical.

## Canonical Ledger Schema Compatibility

The canonical ledger schema has its own schema version, such as:

```text
reckonry-ledger-v1
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

## CLI Compatibility

CLI compatibility covers documented commands, options, exit codes, and output contracts.

Rules:

- New commands and options may be added in minor releases.
- Existing documented commands should remain stable during a major version line.
- Breaking CLI changes require a major version bump after `1.0.0`.
- Validation commands must keep documented success/failure behavior stable.
- Human-readable output may improve, but scripts should not be broken without migration notes.

During alpha releases, CLI commands may still change. Breaking CLI changes should be documented in `CHANGELOG.md` and release notes.

## Importer Compatibility

Importer compatibility covers the relationship between source exports and canonical ledger events.

Rules:

- Importers produce canonical ledger events.
- Importers must preserve source references and raw data.
- New importer coverage should reduce unknown rows, not discard them.
- Changes to event classification, posting direction, amount mapping, or source preservation are compatibility-sensitive.
- Importer behavior changes must include tests and changelog notes.
- If importer output semantics change, migration notes must explain how to regenerate affected ledgers.

## SDK and Plugin Compatibility

Reckonry is designed for importer, pricing, tax, report, audit, and reconciliation modules.

Current alpha compatibility rules:

- Bundled module contracts are implemented but not stable external NuGet SDKs.
- External binary plugin loading is planned, not implemented.
- Breaking SDK contract changes must be documented with migration notes.

Future stable plugin compatibility rules:

- Plugin contracts must be versioned.
- Plugin descriptors must declare identity, version, supported files, schemas, operations, and coverage where applicable.
- Plugins must consume or produce canonical ledger data through documented abstractions.
- Plugins must not mutate existing ledger events.
- Breaking plugin contract changes require a major version bump once plugin contracts are stable.
- Host applications should reject incompatible plugins with clear errors.

## Report Compatibility

Report compatibility covers file names, formats, fields, and reproducibility expectations.

Rules:

- Reports must be reproducible from the canonical ledger and declared inputs.
- Generic reports must remain country-independent and provider-independent.
- Country and professional reports must declare country scope, provider scope where applicable, output formats, and professional review status.
- Breaking changes to report formats require release notes and migration guidance.

## Tax Module Compatibility

Tax modules interpret the ledger without modifying it. Country-specific tax behavior must cite official sources and document assumptions.

Changes that affect tax forms, report fields, monitoring outputs, capital gains behavior, or professional-review artifacts require compatibility notes and professional review where appropriate.

## Changelog and Migration Notes

Every user-visible change should update the changelog.

Breaking changes require migration notes that explain:

- What changed.
- Who is affected.
- How to detect affected files or workflows.
- How to migrate or regenerate outputs.
- Whether older formats remain readable.
