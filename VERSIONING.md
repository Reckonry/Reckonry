# Versioning

Reckonry uses semantic versioning for application releases and explicit compatibility expectations for the CLI, ledger schema, importers, and tax modules.

## App Versioning

Application releases use `MAJOR.MINOR.PATCH` with optional prerelease labels such as `alpha`, `beta.1`, or `rc.1`.

- Major versions may include breaking changes.
- Minor versions add compatible functionality.
- Patch versions fix bugs without intentional compatibility breaks.

## CLI Compatibility

CLI commands may change during alpha releases. Breaking CLI changes should be documented in `CHANGELOG.md` and release notes.

Stable releases should avoid breaking existing command names, option names, output paths, and exit-code behavior without a major version change.

## Ledger Schema Compatibility

The canonical ledger schema is a core compatibility contract. Schema changes must be explicit, documented, and reviewable.

- Compatible changes may add optional fields or validation warnings.
- Breaking changes require migration guidance and a versioned schema update.
- Unknown or unsupported source rows must remain preserved rather than discarded.

## Importer Compatibility

Importer compatibility covers accepted source files, schema variants, operation mappings, and unsupported row handling.

Importers may improve classification over time, but changes that alter generated ledger events should be documented because they can affect reports, reconciliation, and professional review.

## Tax Module Compatibility

Tax modules interpret the ledger without modifying it. Country-specific tax behavior must cite official sources and document assumptions.

Changes that affect tax forms, report fields, monitoring outputs, or capital gains behavior require compatibility notes and professional review where appropriate.
