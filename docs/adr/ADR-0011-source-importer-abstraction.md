# ADR-0011: Source Importer Abstraction

## Status

Accepted

## Context

Reckonry originally centered import terminology around exchanges. That is too
narrow for the intended platform: future inputs may come from brokers, banks,
wallets, custodians, government reports, accounting systems, or manual CSV
files.

## Decision

`ISourceImporter` is the primary importer contract. `SourceKind` describes the
kind of source being imported.

`IExchangeImporter` remains only as a compatibility marker for existing
exchange importers. New importer documentation and host commands should use
source/provider terminology rather than exchange-first terminology.

## Consequences

Reckonry can add non-exchange importers without changing the canonical ledger.

Public SDK documentation must describe `ISourceImporter` as the primary
contract.

Compatibility aliases should not appear as the preferred public command shape.
