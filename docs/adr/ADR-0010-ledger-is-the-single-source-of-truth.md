# ADR-0010: Ledger Is The Single Source Of Truth

## Status

Accepted

## Context

LedgerForge has multiple data inputs and outputs: exchange exports, canonical events, reports, reconciliation summaries, official PDFs, and future tax modules. Without a source-of-truth rule, downstream components could disagree about which artifact controls the system.

## Decision

The canonical ledger is the single source of truth inside LedgerForge.

Reports, reconciliation summaries, and tax outputs are derived artifacts. External reports can validate or challenge the ledger, but they do not replace it.

## Consequences

All workflows remain traceable back to canonical ledger events.

Reconciliation mismatches require explicit review.

Future modules must consume ledger data rather than bypassing it with source-specific shortcuts.
