# ADR-0002: Ledger Is Immutable

## Status

Accepted

## Context

Ledger data is used for audit, reconciliation, accounting review, and tax workflows. Mutation of historical events can make results difficult to reproduce and can hide the source of corrections.

## Decision

Ledger events are immutable records. Corrections must be represented by additional events, revised imports, or explicit derived outputs, not by mutating existing events in place.

## Consequences

Ledger history is reproducible and auditable.

Downstream reports can be regenerated from the same ledger input.

Any future correction workflow must preserve traceability instead of overwriting source-derived events.
