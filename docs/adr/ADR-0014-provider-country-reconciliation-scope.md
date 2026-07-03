# ADR-0014: Provider/Country Reconciliation Scope

## Status

Accepted

## Context

Some reconciliation workflows are generic. Others depend on both a provider and
a country-specific official report format. Binance Italy official reports are a
provider/country concern, not a generic reconciliation concern.

## Decision

Generic reconciliation contracts live in
`Reckonry.Reconciliation.Abstractions`.

Provider/country reconciliation implementations live in scoped modules such as
`Reckonry.Reconciliation.Binance.Italy`.

## Consequences

Generic reconciliation abstractions must not reference Binance, Italy, RW,
Agenzia Entrate, or other provider/country concepts.

Hosts route reconciliation by provider and country, for example:

```bash
reckonry reconcile binance italy --reports <official-pdfs> --ledger-reports <reports> --out <output>
```

Future provider or country reconciliation modules can be added without changing
the generic reconciliation contracts.
