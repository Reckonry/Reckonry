# LedgerForge Canonical Ledger v1

Status: Draft

Schema version: `ledgerforge-ledger-v1`

JSON schema: [`../../ledgerforge.schema.json`](../../ledgerforge.schema.json)

## Purpose

The LedgerForge canonical ledger format is the durable interchange format between importers, reports, reconciliation, audit checks, and future tax modules.

It is factual, source-preserving, exchange-independent, and country-independent. It does not contain tax advice, capital gains calculations, LIFO/FIFO lots, or jurisdiction-specific interpretations.

## Ledger

A ledger document is a JSON object:

```json
{
  "schemaVersion": "ledgerforge-ledger-v1",
  "metadata": {
    "createdAtUtc": "2026-07-02T12:00:00+00:00",
    "generator": "LedgerForge",
    "eventCount": 1
  },
  "events": []
}
```

Fields:

- `schemaVersion`: exact canonical format identifier.
- `metadata`: document-level metadata.
- `events`: ordered or unordered canonical ledger events. Consumers must not assume file order is authoritative; timestamps and source references remain explicit.

## Posting

A posting is one movement of one asset in or out of an account.

Fields:

- `assetSymbol`: non-empty asset symbol as represented by the importer.
- `amount`: positive JSON number. LedgerForge uses `decimal` internally and never `double`.
- `direction`: `In` or `Out`.
- `account`: non-empty logical account label.
- `value`: optional fiat money value attached to the posting.

Direction semantics:

- `In` increases quantity.
- `Out` decreases quantity.

## Asset

Assets are represented by `assetSymbol` on postings and by `AssetAmount` in code.

Rules:

- Asset symbols must not be blank.
- Importers should preserve exchange symbols unless a documented normalization rule exists.
- Unknown assets must not be guessed. If the row cannot be interpreted safely, create an `Unknown` ledger event.

## Money

Money values are represented as:

- `currencyCode`: three-letter uppercase currency code such as `EUR`.
- `amount`: non-negative JSON number.

Money values are factual values from source data or validated enrichment. They are not tax estimates by default.

## SourceReference

Every ledger event must preserve source evidence:

- `sourceSystem`: exchange, importer, or source system.
- `sourceFile`: source file name.
- `sourceRowNumber`: 1-based row number.
- `rawData`: original raw row data or source payload.

Unknown rows must never be discarded. Unsupported source rows must be represented as `LedgerEventType.Unknown` and keep `SourceReference.rawData`.

## Metadata

Required metadata:

- `createdAtUtc`: UTC creation timestamp.
- `generator`: tool name or host that generated the ledger.
- `eventCount`: number of events in `events`.

Metadata may evolve with additive fields. Consumers must ignore unknown metadata fields.

## Versioning

The v1 schema version is `ledgerforge-ledger-v1`.

Breaking changes require a new schema version. Additive metadata fields do not require a new version. Additive event fields require compatibility review and must not change existing semantics.

Readers may support older formats for migration, but validators should validate the declared canonical version.

## Integrity

Canonical format integrity requires:

- Valid JSON object root.
- Exact `schemaVersion`.
- `metadata.eventCount` equal to `events.length`.
- Non-empty source references on every event.
- Decimal-safe JSON numbers for asset and money amounts.
- Valid event type and posting direction enum values.
- Positive posting quantities.
- Non-negative money values.

## Audit

Audit reports are separate outputs. They consume the canonical ledger and never modify it.

Audit checks may include duplicate transactions, broken transfers, missing assets, negative balances, unknown ratios, timestamp anomalies, currency anomalies, fee anomalies, and missing source references.

## Schema Evolution

Schema evolution rules:

- Preserve source references forever.
- Preserve unknown rows forever.
- Do not infer tax meaning in the canonical ledger.
- Prefer additive changes.
- Version breaking changes explicitly.
- Keep readers backward-compatible where practical.
- Keep reports and reconciliation read-only relative to the ledger.

## JSON Schema

The machine-readable JSON schema is stored at:

```text
ledgerforge.schema.json
```

The CLI validator uses the same canonical constraints and returns either:

```text
PASS
```

or:

```text
Validation errors:
- $.path.to.field error
```
