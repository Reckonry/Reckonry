# ADR-0004: Reports Never Modify Ledger

## Status

Accepted

## Context

Reports are derived views over the canonical ledger. If report generation mutated ledger data, report execution order could affect results.

## Decision

Reports are read-only consumers of the canonical ledger. Reports may generate files, warnings, summaries, and diagnostics, but must never modify ledger events.

## Consequences

Reports are repeatable and deterministic for the same input.

Different reports can be generated independently.

Report warnings must describe issues without silently repairing ledger data.
