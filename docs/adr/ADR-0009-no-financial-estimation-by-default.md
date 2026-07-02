# ADR-0009: No Financial Estimation By Default

## Status

Accepted

## Context

Missing prices, missing fiat values, unknown operation types, and incomplete export rows are common in crypto data. Automatically estimating values can create false confidence and accidental tax interpretation.

## Decision

LedgerForge does not perform financial estimation by default.

When values are missing, LedgerForge must report warnings or gaps instead of inventing prices, balances, tax values, or gains.

## Consequences

Outputs are conservative and auditable.

Users can distinguish source-provided data from missing data.

Future pricing providers must be explicit dependencies and must surface provenance and warnings.
