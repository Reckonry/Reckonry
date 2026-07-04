# Reckonry Vision

Reckonry exists because crypto accounting depends on data that is difficult to verify.

Exchange exports are inconsistent. Wallet histories are fragmented. Reports often hide assumptions. Tax tools frequently combine parsing, classification, valuation, and jurisdiction-specific logic into systems that are hard to inspect. When a generated number is wrong, it can be difficult to understand which source row caused the problem, which rule was applied, or whether missing data was silently ignored.

Reckonry is an engineering response to that problem.

Its purpose is to reconstruct a canonical, auditable ledger from imperfect crypto data while preserving uncertainty instead of hiding it. The ledger is factual infrastructure. Reports, audit checks, reconciliation tools, pricing providers, and tax modules consume it, but they do not replace it.

## Why Reckonry Exists

Crypto accounting needs a reviewable intermediate representation between raw exports and final reports.

Raw exchange data is not enough because:

- Different exchanges use different schemas.
- The same exchange changes export formats over time.
- CSV files often omit context.
- Related activity may be split across products, accounts, files, and years.
- Wallet data may require interpretation across chains and addresses.
- Fees, rewards, conversions, transfers, and internal movements are represented inconsistently.

Final tax or accounting reports are not enough because:

- They may not show enough provenance.
- They may merge assumptions with facts.
- They may be difficult to reproduce.
- They may not preserve unknown or unsupported rows.
- They may be jurisdiction-specific and unsuitable as a universal source of truth.

Reckonry exists to sit between those two layers.

It turns raw source data into a canonical ledger where every event, posting, and generated number can be traced, reviewed, tested, audited, and reproduced.

## Problems It Addresses

### Source Fragmentation

Crypto activity is spread across centralized exchanges, wallets, custody providers, DeFi protocols, internal transfers, reward systems, and fiat rails. Reckonry aims to normalize those sources into one ledger model without erasing source-specific evidence.

### Format Instability

Import formats change. Columns are renamed. New operations appear. Existing operations gain new meanings. Reckonry treats importer coverage as explicit metadata and preserves unknown rows so unsupported schemas can be reviewed and improved.

### Hidden Uncertainty

Unknown data is part of the record. A system that discards unknown rows creates false confidence. Reckonry represents unknown data explicitly and makes it visible in reports, audit checks, and exception files.

### Weak Provenance

Professional review requires evidence. Reckonry keeps source system, source file, row number, and raw data attached to ledger events so reviewers can trace output back to input.

### Mixed Responsibilities

Parsing is not tax logic. Reporting is not ledger mutation. Reconciliation is not source-of-truth replacement. Reckonry keeps these responsibilities separate so each part can be tested and reasoned about independently.

### Reproducibility

A report should be reproducible from the same ledger and the same options. Reckonry treats reproducibility as a core requirement, not a convenience.

## Target Users

### Professional Accountants

Accountants need reviewable records, clear warnings, source traceability, reproducible reports, and explicit uncertainty. Reckonry should help them inspect what happened without relying on opaque transformations.

Reckonry does not replace professional judgment. It provides structured evidence and deterministic reports that professionals can validate.

### Developers

Developers need stable contracts for building importers, reports, reconciliation modules, pricing providers, storage adapters, and tax modules. Reckonry should provide a clean architecture where third-party code can plug in without depending on private internals.

The SDK direction is designed for developers who need to support additional exchanges, wallets, countries, report formats, or company workflows.

### Auditors

Auditors need traceability, immutability, reproducibility, and clear separation between facts and interpretations. Reckonry should allow audit workflows to inspect the canonical ledger, source references, unknown rows, report generation logic, and reconciliation status.

Auditability must not depend on access to undocumented behavior.

### Companies

Companies need a ledger engine that can fit into controlled workflows. They may need proprietary integrations, internal review processes, multiple entities, multiple jurisdictions, and clear evidence trails.

Reckonry should be usable as open-source infrastructure and as a foundation for commercial integrations where AGPL obligations are not acceptable.

## Long-Term Direction

### Add More Source Importers

Reckonry should support additional source importers over time. Each importer should publish supported files, schemas, operations, version, and coverage.

Importer coverage should improve without sacrificing source preservation. Unsupported rows should remain visible until they are understood.

### Add Wallet Importers

Wallet support is a long-term direction. Wallet importers will need to handle chain-specific data, address ownership, gas fees, token transfers, contract interactions, staking, bridging, and protocol events.

Wallet importers must follow the same rules as exchange importers: preserve source evidence, avoid invented data, and surface uncertainty.

### Support Multiple Countries

Tax modules should be country-specific and separate from Core. They should consume the canonical ledger and produce jurisdiction-specific interpretations without mutating ledger events.

Supporting multiple countries requires:

- Stable ledger semantics.
- Clear module boundaries.
- Jurisdiction-specific test fixtures.
- Explicit assumptions.
- Migration notes when rules or report semantics change.

### Document Ledger Interchange

Reckonry should evolve toward a documented interchange format for digital asset ledger data.

The canonical ledger format should be:

- Exchange-independent.
- Wallet-independent.
- Country-independent.
- Source-preserving.
- Versioned.
- Validatable.
- Suitable for long-term storage and review.

The goal is not to force every workflow into one tool. The goal is to create a shared factual format that other tools can consume and produce.

### Improve Ledger Reconstruction Quality

Reckonry should keep improving as an engine for reconstructing crypto accounting ledgers from messy source data.

That means:

- Deterministic imports.
- Explicit unknown data.
- Strong validation.
- Auditable reports.
- Reconciliation against external evidence.
- Clear SDK boundaries.
- Repeatable test fixtures.
- Transparent assumptions.

The goal is not infallibility. The goal is behavior that is documented, testable, explainable, and open to review.

## Guiding Principles

- Never invent financial data.
- Never hide unknown information.
- Every imported byte must remain traceable.
- Every generated number must be explainable.
- Every report must be reproducible.
- The ledger is the only source of truth.
- Tax modules interpret the ledger. They never modify it.
- Parsing and tax logic must remain separated.
- Correctness is more important than performance.
- Auditability is more important than convenience.
- Decimal arithmetic is required for financial and crypto amounts.
- UTC is required internally for canonical event timestamps.
- Privacy is the default.
- Real financial data must never be committed.

## Engineering Direction

Reckonry should continue to grow around these architecture boundaries:

- `Reckonry.Core`: canonical ledger models only.
- `Reckonry.Importers.Abstractions`: importer plugin contracts.
- `Reckonry.Importers.*`: exchange and wallet-specific importers.
- `Reckonry.Reports`: read-only reports from the canonical ledger.
- `Reckonry.Audit`: read-only integrity checks.
- `Reckonry.Reconciliation.Abstractions`: generic reconciliation contracts.
- `Reckonry.Reconciliation.*`: provider or country scoped reconciliation modules.
- `Reckonry.Tax.Abstractions`: tax module contracts.
- `Reckonry.Tax.*`: country-specific tax modules.
- `Reckonry.Pricing.Abstractions`: pricing provider contracts.
- `Reckonry.Storage`: persistence and schema validation.
- `Reckonry.Cli`: command-line host and workflow entry point.

These boundaries should remain boring, explicit, and defensible. Reckonry should prefer clear interfaces, deterministic behavior, and testable transformations over hidden automation.

## What Reckonry Should Avoid

Reckonry should avoid:

- Opaque calculations.
- Silent data loss.
- Tax logic inside importers.
- Ledger mutation by reports or tax modules.
- Financial estimation by default.
- Source data committed to the repository.
- Convenience features that weaken auditability.
- Performance optimizations that make behavior harder to verify.

## Long-Term Measure Of Success

Reckonry succeeds if a reviewer can answer these questions for any generated report:

- Which source files were imported?
- Which rows became which ledger events?
- Which rows were unknown?
- Which calculations produced each number?
- Which assumptions were applied?
- Which version of the schema, importer, report, and module produced the output?
- Can the same output be reproduced from the same ledger?

If those questions can be answered without hidden behavior, Reckonry is serving its purpose.
