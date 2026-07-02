# ADR-0005: Reconciliation Is Read-Only

## Status

Accepted

## Context

LedgerForge can compare its internally reconstructed ledger reports with official exchange-issued reports. These official reports are validation evidence, not ledger sources of truth.

## Decision

Reconciliation is read-only. It compares LedgerForge outputs with external reports and produces summaries, statuses, warnings, and diagnostics. It must never replace, mutate, or backfill ledger events.

## Consequences

The canonical ledger remains the source of truth.

Official exchange reports can validate or challenge LedgerForge output without overriding it.

Any mismatch requires explicit user review or future correction workflows.
