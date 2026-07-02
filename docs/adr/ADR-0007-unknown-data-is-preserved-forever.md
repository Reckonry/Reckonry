# ADR-0007: Unknown Data Is Preserved Forever

## Status

Accepted

## Context

Exchange exports can contain unsupported rows, new operation types, malformed values, or data that cannot be safely interpreted. Dropping unknown data would compromise auditability.

## Decision

Unknown data must be preserved forever. Unsupported rows become explicit unknown ledger events or exception records with source references and raw data.

## Consequences

LedgerForge prefers visible uncertainty over silent data loss.

Reports and reconciliation can warn about unknowns.

Importer improvements can later reclassify supported formats from the original preserved evidence.
