# ADR-0001: Canonical Ledger

## Status

Accepted

## Context

LedgerForge imports data from many exchanges, wallets, brokers, and future data sources. Each source can use different formats, field names, export semantics, timestamps, and asset conventions.

Without a canonical model, reports, reconciliation, tax modules, and validation tools would need to understand every source-specific shape.

## Decision

LedgerForge will normalize all imported activity into a canonical ledger model in `LedgerForge.Core`.

Importers produce canonical ledger events. Reports, reconciliation, tax modules, storage, and other downstream components consume canonical ledger events.

## Consequences

Provider-specific complexity stays inside importer plugins.

Core remains exchange-independent and country-independent.

Any information that cannot be confidently normalized must still be represented in the canonical ledger as unknown or exceptional data, not discarded.
