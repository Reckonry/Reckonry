# Changelog

All notable changes to Reckonry will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project follows semantic versioning as described in [VERSIONING.md](VERSIONING.md).

## [Unreleased]

### Added

- Repository governance documents, issue templates, pull request template, and GitHub Actions workflows.
- Public fake demo workflow with synthetic source data and generated local artifacts under `artifacts/demo/`.
- Product showcase screenshots generated from demo outputs.
- Global-readiness and public-release review documents.
- Bundled assembly plugin discovery catalog for importers, tax modules, reports, reconciliation modules, and pricing providers.
- Generic reconciliation abstraction project and Binance Italy provider/country reconciliation project.
- Architecture boundary tests and plugin discovery tests.
- ADRs for source importers, bundled assembly discovery, country report isolation, and provider/country reconciliation scope.
- Release notes template for `v0.1.0-alpha`.

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

## [0.1.0-alpha] - TBD

### Added

- Canonical ledger foundations.
- Binance importer foundation.
- Italy RW accountant report foundation.
- Tax Dossier foundation.
- Audit package foundation.
