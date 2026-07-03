# ADR-0013: Country Reports Live In Country Modules

## Status

Accepted

## Context

RW reports are Italy-specific. Keeping RW report writers in generic report
projects made Reckonry look Italy-centric and risked leaking country concepts
into generic reporting.

## Decision

Generic reports belong in `Reckonry.Reports`.

Country-specific reports belong in country modules such as
`Reckonry.Tax.Italy`.

Italy RW snapshot, value, accountant package, and Tax Dossier generation live
under `Reckonry.Tax.Italy.Rw`.

## Consequences

`Reckonry.Reports` must not reference Italy, RW, Agenzia Entrate, Binance, EUR,
or other country/provider concepts.

New country modules, such as a future `Reckonry.Tax.Spain`, can add their own
reports without modifying Italy report code.

Report descriptors must advertise country/provider/professional scope.
