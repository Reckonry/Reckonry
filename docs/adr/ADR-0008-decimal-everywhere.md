# ADR-0008: Decimal Everywhere

## Status

Accepted

## Context

Financial and crypto quantities require deterministic decimal behavior. Binary floating point types can introduce rounding artifacts that are unacceptable for ledger, accounting, reconciliation, and tax workflows.

## Decision

Reckonry uses `decimal` for financial amounts, crypto quantities, fiat values, and derived report quantities.

`double` and `float` must not be used for monetary or asset quantities.

## Consequences

Calculations are more predictable for financial workflows.

Performance tradeoffs are acceptable for auditability and correctness.

External data parsed from CSV, PDF, APIs, or storage must be converted into decimal-safe representations before entering ledger logic.
