# ADR-0003: Importers Never Modify Existing Events

## Status

Accepted

## Context

Importers translate source exports into canonical ledger events. If importers also modified existing events, import behavior would become non-local and hard to audit.

## Decision

Importers only produce new canonical ledger events from source data. They must not modify, reinterpret, or delete existing ledger events.

## Consequences

Importers remain simple, plugin-ready, and source-focused.

Deduplication, reconciliation, and correction workflows must be explicit separate processes.

Original source references remain attached to imported events.
