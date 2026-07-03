# ADR-0006: Tax Modules Consume Ledger Only

## Status

Accepted

## Context

Tax rules vary by jurisdiction and change over time. Mixing tax interpretation into importers or core ledger reconstruction would make the ledger country-specific and harder to reuse.

## Decision

Tax modules consume canonical ledger events. They do not own import logic and do not mutate the ledger.

Tax modules must live outside `Reckonry.Core`.

## Consequences

Core remains country-independent.

Tax modules can evolve per country without changing importer semantics.

Tax output is always derived from ledger data and explicit jurisdiction-specific rules.
