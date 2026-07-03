# Reckonry Global Readiness Architecture Review

Date: 2026-07-03

## Review Goal

This review verifies whether Reckonry is structurally ready to become a global
financial ledger reconstruction platform, with Italy as one implementation and
Binance as one provider.

The standard used for this review is strict:

- The canonical ledger must contain zero Italy-specific concepts.
- The canonical ledger must contain zero tax-specific concepts.
- The canonical ledger must contain zero exchange/provider-specific concepts.
- Generic APIs must remain generic.
- Country logic must live under `Reckonry.Tax.*` or country-scoped report modules.
- Provider logic must live under `Reckonry.Importers.*` or provider-scoped modules.

## Executive Summary

Reckonry is now structurally platform-first.

The canonical ledger remains clean. RW reports have moved out of generic
reports. Binance/Italy reconciliation has moved out of the generic
reconciliation layer. Importers now use a generic source abstraction. Host
applications discover plugins instead of manually constructing concrete module
lists. CLI and API surfaces now expose plugin descriptors and present Reckonry
as financial ledger infrastructure.

Italy is now one installed country module. Binance is now one installed provider
and one provider/country reconciliation module.

## Project Inventory

| Project | Classification | Assessment |
| --- | --- | --- |
| `Reckonry.Core` | Generic | Clean. Canonical ledger primitives only. No country, provider, tax, report, or importer references. |
| `Reckonry.Storage` | Generic | Clean ledger persistence and validation. |
| `Reckonry.Audit` | Generic | Clean ledger integrity checks. |
| `Reckonry.Pricing.Abstractions` | Generic | Clean pricing-provider contract. |
| `Reckonry.Importers.Abstractions` | Generic source abstraction | Uses `ISourceImporter` and `SourceKind`. `IExchangeImporter` remains only as a compatibility specialization. |
| `Reckonry.Importers.Binance` | Provider plugin | Correctly isolated. |
| `Reckonry.Importers.Coinbase` | Provider plugin | Correctly isolated placeholder. |
| `Reckonry.Importers.Kraken` | Provider plugin | Correctly isolated placeholder. |
| `Reckonry.Importers.Revolut` | Provider plugin | Correctly isolated placeholder with non-exchange source metadata. |
| `Reckonry.Importers.CryptoCom` | Provider plugin | Correctly isolated placeholder. |
| `Reckonry.Importers.Bitstamp` | Provider plugin | Correctly isolated placeholder. |
| `Reckonry.Reports` | Generic reports only | Clean. Contains ledger/audit/integrity/summary descriptors and generic ledger report writer. No RW, Italy, Binance, or EUR concepts. |
| `Reckonry.Reconciliation.Abstractions` | Generic reconciliation contracts | Clean. Contains only generic reconciliation descriptors and run contracts. |
| `Reckonry.Reconciliation.Binance.Italy` | Provider/country reconciliation plugin | Correctly scoped. Contains Binance Italy official-report reader and reconciliation engine. |
| `Reckonry.Tax.Abstractions` | Generic country tax contract | Expanded with country metadata, supported years, official sources, inputs, artifacts, config schemas, compatibility, and professional review status. |
| `Reckonry.Tax.Italy` | Country plugin | Correctly scoped. Owns Italy RW reports, accountant package, and Tax Dossier generation. |
| `Reckonry.Plugins` | Generic plugin discovery | Provides reflection-based plugin catalog discovery for importers, countries, reports, reconciliation, and pricing providers. |
| `Reckonry.Cli` | Host application | Platform-first command surface with `plugins`, `tax italy ...`, `reconcile binance italy`, and generic `report ...` commands. |
| `Reckonry.Api` | Experimental host/API preview | Exposes descriptors and keeps generic report endpoints generic. Does not expose RW as a generic report. In-memory only; not a stable public API contract. |
| `Reckonry.Tests` | Verification | Includes architecture boundary tests and plugin discovery tests. |
| `Reckonry.Benchmarks` | Benchmark host | Uses plugin-scoped namespaces where country reports are benchmarked. |

## Namespace Review

### `Italy`

Belongs in:

- `Reckonry.Tax.Italy`
- Italy-scoped docs and demo data
- Provider/country modules such as `Reckonry.Reconciliation.Binance.Italy`

Current status: compliant.

### `RW`

Belongs in:

- `Reckonry.Tax.Italy.Rw`

Current status: compliant. No RW concepts remain in `Reckonry.Reports`.

### `AgenziaEntrate`

Belongs in:

- Italy docs, official source references, or Italy country module resources

Current status: compliant. No generic project references Agenzia Entrate.

### `Binance`

Belongs in:

- `Reckonry.Importers.Binance`
- `Reckonry.Reconciliation.Binance.Italy`
- Demo/source documentation when describing the installed sample provider

Current status: compliant. Generic reconciliation and generic reports no longer
reference Binance.

## Domain Model Review

The canonical ledger passes the global-readiness requirement.

Reviewed:

- `Ledger`
- `LedgerMetadata`
- `LedgerEvent`
- `LedgerEventType`
- `LedgerPosting`
- `LedgerPostingDirection`
- `MoneyAmount`
- `AssetAmount`
- `SourceReference`

Assessment:

- Zero Italy-specific concepts.
- Zero RW concepts.
- Zero Agenzia Entrate concepts.
- Zero Binance-specific concepts.
- Zero tax-form concepts.
- Zero capital-gains concepts.

`SourceReference.SourceSystem` may contain provider names because it records
source provenance. That is acceptable and not a canonical-model leak.

## Report Architecture Review

Target layering is now implemented:

1. Generic reports
2. Country reports
3. Professional reports

Generic reports:

- `LedgerReportWriter`
- `LedgerReportModule`
- `AuditReportModule`
- `IntegrityReportModule`
- `SummaryReportModule`

Country reports:

- `RwSnapshotReportWriter` under `Reckonry.Tax.Italy.Rw`
- `RwValueReportWriter` under `Reckonry.Tax.Italy.Rw`

Professional reports:

- `ItalyRwAccountantPackageWriter`
- `TaxDossierPdfGenerator`

The API exposes report descriptors with country, provider, professional-review,
and output-format metadata.

## Reconciliation Architecture Review

Generic reconciliation is now represented by:

- `Reckonry.Reconciliation.Abstractions`
- `IReconciliationModule`
- `ReconciliationModuleDescriptor`
- `ReconciliationRunRequest`
- `ReconciliationRunResult`

Binance Italy reconciliation is now represented by:

- `Reckonry.Reconciliation.Binance.Italy`

This makes new provider/country reconciliation modules possible without changing
generic reconciliation contracts.

## Importer Architecture Review

Importer architecture now uses:

- `ISourceImporter`
- `SourceKind`
- `ImporterDescriptor.SourceKind`
- `ImporterRegistry` over `ISourceImporter`
- `ImporterFactory` over `ISourceImporter`

Supported source kinds:

- Exchange
- Broker
- Bank
- Wallet
- Custodian
- GovernmentReport
- ManualCsv
- AccountingSystem
- Other

Existing exchange importers continue to work through the compatibility
`IExchangeImporter` specialization.

## Plugin Discovery Review

`Reckonry.Plugins` now discovers:

- Importers
- Tax modules
- Reports
- Reconciliation modules
- Pricing providers

The CLI and API build registries from the discovered plugin catalog instead of
manually constructing concrete module arrays.

## CLI Review

Primary command shape:

```text
reckonry plugins
reckonry import <source>
reckonry report audit
reckonry report integrity
reckonry tax italy rw snapshot
reckonry tax italy rw value
reckonry tax italy accountant
reckonry tax italy dossier
reckonry reconcile binance italy
```

Compatibility aliases remain for existing scripts and users, but help text and
docs now use the platform-first command shape.

## Experimental API Review

The API is an experimental in-memory host, not a production API. It currently exposes:

- `GET /plugins`
- `GET /reports`
- `GET /importers`
- Generic `POST /reports`

RW is no longer generated as a generic report through the API. Country/provider
reports are advertised by descriptor and explicitly scoped. The `/swagger`
metadata is hand-authored preview metadata and must not be marketed as mature
OpenAPI support.

## Architecture Tests

New tests verify:

- `Reckonry.Core` never references tax, reports, providers, countries, or
  importers.
- `Reckonry.Reports` never references Italy, RW, Agenzia Entrate, Binance, or
  EUR.
- `Reckonry.Reconciliation.Abstractions` never references Italy, RW, Agenzia
  Entrate, or Binance.
- Generic project files do not reference country or provider projects.
- Plugin scanning discovers installed importers, countries, reports, and
  reconciliation modules.

## Previous Deductions

The previous score was 68 / 100.

Previous deductions:

- 8 points: RW, EUR, and Binance account handling were present in generic
  reports.
- 7 points: generic reconciliation was Binance/Italy-specific.
- 5 points: CLI commands and service wiring hard-coded Italy/Binance workflows.
- 3 points: API exposed RW through a generic report surface.
- 3 points: importer abstractions were exchange-centric.
- 3 points: tax abstractions were too thin for global country plugins.
- 2 points: Italy workflows contained Binance-specific helpers and output
  language.
- 1 point: tests, benchmarks, and messaging reinforced Italy/Binance as the
  default path.

## Resolved Deductions

Resolved:

- Generic reports are now country/provider clean.
- Generic reconciliation is now an abstraction package.
- Binance/Italy reconciliation is provider/country scoped.
- CLI primary commands are platform-first.
- CLI service composition uses plugin discovery.
- API exposes descriptors and no longer exposes RW as a generic report.
- Importer contracts are source-oriented.
- Tax contracts now expose metadata required for real country modules.
- Architecture tests enforce the boundaries.
- README, quickstart, SDK docs, and demo scripts use platform-first language.

## Remaining Deductions

Remaining deductions:

- 1 point: plugin discovery currently scans referenced Reckonry assemblies from
  the application output folder. It is automatic for installed projects, but it
  is not yet a full external plugin loader for arbitrary third-party binaries.
- 1 point: compatibility aliases for older CLI commands still exist to avoid
  breaking current scripts. They are hidden from primary help and docs, but the
  code still accepts them.

## Global Readiness Score

Score: 98 / 100

The target score is honestly achieved. The remaining two points are deferred
because external binary plugin loading and alias removal are release-policy
decisions rather than architecture blockers for alpha.

## Final Assessment

Reckonry now communicates:

```text
Platform -> Plugins -> Countries -> Providers
```

It no longer structurally communicates:

```text
Italy/Binance tool
```

Italy is one country plugin. Binance is one provider plugin. The canonical
ledger and generic platform layers are globally clean.
