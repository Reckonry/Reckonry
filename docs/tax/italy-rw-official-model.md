# Italy RW official model

LedgerForge includes a draft official Quadro RW model for crypto-assets in
`LedgerForge.Tax.Italy`.

Source of truth: [docs/analysis/quadro-rw-analysis.md](../analysis/quadro-rw-analysis.md).

This model is an engineering representation of RW fields. It is not tax, legal,
accounting, or financial advice.

## Scope

Implemented:

- RW1-RW5 crypto line model for columns 1-21 and 29-34.
- RW8 crypto-assets summary model for columns 1-6.
- Taxpayer/report configuration for ownership, possession type, ownership
  percentage, co-owners, prior credits, F24 compensations, advances paid, and
  monitoring-only flag.
- Valuation evidence records for exchange values, analogous platform values,
  market data site values, and acquisition cost fallback.
- Draft crypto line generation for configured crypto asset symbols.
- Validation messages that block finalization when required data is missing.
- Accountant review package generation with markdown, CSV, and JSON outputs.
- Private Italy RW config template generation under ignored local input folders.

Not implemented:

- Final tax advice.
- RT capital gains.
- LIFO/FIFO or cost-basis logic.
- Automatic market price sourcing.
- Automatic taxpayer ownership inference.
- Filing recommendations.

## Validation behavior

The report is not finalizable when:

- Ownership title is missing.
- Ownership percentage is missing or invalid.
- Crypto asset symbols are not explicitly configured.
- Valuation evidence is missing.
- Unknown ledger events may affect configured crypto balances.

The report emits a warning when RW foreign-state treatment is ambiguous. The
official instructions say the foreign state code is not mandatory for virtual
currencies, but they do not define one canonical treatment for exchange custody,
self-custody, or multi-venue assets.

## Accountant review package

Before creating the accountant package, generate a private config template:

```bash
ledgerforge config italy-rw-template --year 2025 --ledger ./output/ledger.json --out ./input/italy-rw/italy-rw-2025.json
```

If Binance reconciliation output is available, LedgerForge can attempt a
conservative fill:

```bash
ledgerforge config italy-rw-fill-binance --config ./input/italy-rw/italy-rw-2025.json --reconciliation ./output/reconciliation/reconciliation-summary.json --out ./input/italy-rw/italy-rw-2025.binance-filled.json
```

Values are filled only when official Binance report fields are unambiguous.
Otherwise, placeholders stay null and warnings are recorded in the private
config file.

The CLI can generate a professional review package:

```bash
ledgerforge report italy-rw-accountant --input ./output/ledger.json --year 2025 --out ./output/accountant
```

The command writes:

- `italy-rw-accountant-2025.md`
- `italy-rw-accountant-2025.csv`
- `italy-rw-accountant-2025.json`

The package includes RW crypto draft lines, RW8 summary, validation messages,
valuation evidence summary, ledger hash, source file summary, and Binance
reconciliation status when a sibling `output/reconciliation/reconciliation-summary.json`
file exists.

If required taxpayer configuration or valuation evidence is missing, the package
is marked `NOT READY FOR FILING`.

## Calculation assumptions

Draft IC calculation uses:

```text
column 33 = column 8 * ownership quota * holding days / year days * 0.20%
column 34 = column 33 - allowed foreign patrimonial tax credit
```

Holding days are counted in UTC. A day counts when the asset balance is positive
at any point during that UTC calendar day.

Values are accepted only from supplied valuation evidence. LedgerForge does not
invent missing valuation, ownership, credit, compensation, or advance values.

## Data boundary

The canonical ledger remains the source of truth for events and postings. The
Italy RW model consumes the ledger and produces a draft report projection. It
does not modify ledger events.
