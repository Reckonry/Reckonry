# Architecture Decision Records

LedgerForge uses Architecture Decision Records (ADRs) to document significant architectural decisions.

ADRs follow the Michael Nygard format:

- **Status**
- **Context**
- **Decision**
- **Consequences**

Each ADR is immutable once accepted. If a decision changes, create a new ADR that supersedes the old one instead of rewriting history.

## Index

- [ADR-0001 Canonical Ledger](ADR-0001-canonical-ledger.md)
- [ADR-0002 Ledger is immutable](ADR-0002-ledger-is-immutable.md)
- [ADR-0003 Importers never modify existing events](ADR-0003-importers-never-modify-existing-events.md)
- [ADR-0004 Reports never modify Ledger](ADR-0004-reports-never-modify-ledger.md)
- [ADR-0005 Reconciliation is read-only](ADR-0005-reconciliation-is-read-only.md)
- [ADR-0006 Tax modules consume Ledger only](ADR-0006-tax-modules-consume-ledger-only.md)
- [ADR-0007 Unknown data is preserved forever](ADR-0007-unknown-data-is-preserved-forever.md)
- [ADR-0008 Decimal everywhere](ADR-0008-decimal-everywhere.md)
- [ADR-0009 No financial estimation by default](ADR-0009-no-financial-estimation-by-default.md)
- [ADR-0010 Ledger is the single source of truth](ADR-0010-ledger-is-the-single-source-of-truth.md)
