# Changelog

All notable changes to Reckonry will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project follows semantic versioning as described in [VERSIONING.md](VERSIONING.md).

## [Unreleased]

No unreleased changes yet.

## [0.2.0-alpha] - 2026-07-04

### Added

- Coinbase CSV importer for synthetic demo data, canonical ledger generation, unknown row preservation, and plugin discovery.
- Coinbase provider-level reconciliation module for synthetic aggregate statement metadata.
- Public demo lane for Coinbase import, validation, audit, and provider reconciliation.
- Official alpha `dotnet new` plugin templates for importers, tax modules, reports, and reconciliation modules.
- `reckonry doctor` diagnostics for environment, plugin, privacy, SDK, demo, and repository health checks.
- `reckonry explain` provenance command for tracing supported generated artifacts back to source rows, ledger events, postings, and report rows.
- Public artifact golden tests for CLI output, demo output, audit/reconciliation/accountant reports, RW CSV/JSON, Tax Dossier PDF structure, and README command examples.

### Changed

- CLI help, version, success, warning, error, and next-action output now use a more consistent command-line presentation.
- README, architecture, importer SDK, and reconciliation SDK documentation now describe the Coinbase alpha importer/reconciliation path and plugin templates.

### Alpha Scope

This release extends the first public alpha with a second synthetic provider lane and stronger contributor/developer safeguards.

Included:

- Coinbase CSV importer for supported synthetic demo and fixture schemas.
- Coinbase provider-level reconciliation against synthetic aggregate statement metadata.
- Expanded demo workflow that runs Binance Italy and Coinbase synthetic paths.
- Source-based `dotnet new` templates for importer, tax module, report, and reconciliation projects.
- CLI diagnostics through `reckonry doctor`.
- CLI provenance explanations through `reckonry explain` for structured supported artifacts.
- Snapshot/golden tests for public user-facing outputs.

Known limitations:

- Alpha software; APIs, schemas, CLI output, report layouts, and extension contracts may change before `v1.0.0`.
- Not a tax calculator, filing product, accounting system, hosted service, or legal/tax advice.
- The Coinbase importer covers synthetic demo data and selected CSV fixtures only; it is not complete Coinbase product coverage.
- Coinbase reconciliation validates synthetic aggregate statement metadata only; it does not verify balances, valuations, tax treatment, or official Coinbase statements.
- The Binance Italy workflow remains the only complete provider/country professional-review demo.
- `reckonry explain` supports structured artifacts such as ledger JSON, integrity JSON, and RW JSON/CSV; PDF explanation is intentionally not supported.
- Plugin templates are alpha source templates, not stable NuGet packages or stable external binary plugin contracts.
- No NuGet packages are published for this release.

## [0.1.0-alpha] - 2026-07-04

### Added

- Repository governance documents, issue templates, pull request template, and GitHub Actions workflows.
- Public fake demo workflow with synthetic source data and generated local artifacts under `artifacts/demo/`.
- Product showcase screenshots generated from real demo outputs.
- Public alpha release checklist under `docs/reviews/`.
- Bundled assembly plugin discovery catalog for importers, tax modules, reports, reconciliation modules, and pricing providers.
- Generic reconciliation abstraction project and Binance Italy provider/country reconciliation project.
- Architecture boundary tests and plugin discovery tests.
- ADRs for source importers, bundled assembly discovery, country report isolation, and provider/country reconciliation scope.
- Canonical release notes and GitHub release description for `v0.1.0-alpha`.

### Changed

- README links now point contributors to the project governance, roadmap, security, contributing, and changelog documents.
- Public messaging now describes Reckonry as reviewable financial ledger infrastructure rather than a tax calculator.
- README badges and claims were reduced to implemented capabilities.
- RW report writers moved out of generic reports and into the Italy tax module.
- CLI examples now use platform-first command groups such as `tax italy ...` and `reconcile binance italy`.
- Importer abstractions now use `ISourceImporter` and `SourceKind`; `IExchangeImporter` remains as a compatibility specialization.
- SDK and architecture documentation updated around the current bundled plugin architecture.
- Security reporting now points to GitHub private vulnerability reporting for the repository.
- Release workflow now generates SHA256 checksums for CLI artifacts.
- Quickstart now includes expected output snippets and troubleshooting.
- API messaging now marks `Reckonry.Api` as experimental and not a supported public alpha surface.
- Repository documentation was consolidated so README links point to canonical docs only.
- Stale planning docs, decorative unused assets, and old generated benchmark output were removed from the public tree.
- Official source PDF filenames were normalized to lowercase kebab-case with provenance notes.
- Public showcase screenshots and brand assets were refreshed to use current alpha wording.
- Case-only path names were normalized for public Git hosting.

### Public Alpha Scope

This release introduces the first public alpha of Reckonry as CLI-first, local-first financial ledger infrastructure for reviewable digital asset records.

Included:

- Canonical ledger foundations using the Reckonry canonical ledger v1 schema.
- Binance normalized CSV importer demo workflow.
- Ledger validation, audit, integrity, and summary reports.
- Binance Italy reconciliation module for comparing generated outputs with synthetic official-report inputs.
- Italy RW professional-review outputs, including RW snapshot/value reports and accountant package files.
- Tax Dossier PDF generation for professional review.
- Bundled plugin discovery for importers, country tax modules, reports, reconciliation modules, and pricing providers.
- Synthetic public demo under `samples/demo/`.
- Privacy and security posture for local-first processing, ignored private folders, no telemetry, and fake public samples.

Known limitations:

- Alpha software; APIs, schemas, and CLI output may change before `v1.0.0`.
- Not a tax calculator, filing product, accounting system, hosted service, or legal/tax advice.
- CLI is the supported public alpha surface; `Reckonry.Api` is experimental.
- Demo proves one complete provider/country workflow only: Binance Italy with synthetic data.
- Placeholder importer projects are discoverable but not supported parser implementations.
- No NuGet packages are published for this release.
- Professional review outputs are not ready for filing without qualified human review and real evidence supplied outside the repository.
