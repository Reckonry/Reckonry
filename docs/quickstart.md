# Quickstart

This quickstart runs the public Reckonry demo in about 10 minutes on a clean checkout.

The demo uses synthetic data only. It does not use real exchange exports, wallet addresses, account identifiers, private keys, taxpayer data, or private financial records.

## Prerequisites

- .NET SDK 10.0 or newer compatible preview/runtime for `net10.0`.
- Bash on macOS/Linux, or PowerShell on Windows.
- No secrets, database, external service, or network access are required for the demo workflow after dependencies are restored.

## Build

```bash
dotnet build Reckonry.sln
```

## Test

```bash
dotnet test Reckonry.sln
```

## Run The Demo

On macOS/Linux:

```bash
scripts/demo.sh
```

On Windows PowerShell:

```powershell
./scripts/demo.ps1
```

The scripts read synthetic inputs from `samples/demo/` and regenerate outputs under `artifacts/demo/`.

The CLI may print privacy warnings because the public demo intentionally reads from tracked `samples/demo/` files and writes inside the repository. That is expected for this synthetic demo. Real private data must stay in ignored local folders and must never be committed.

## What The Demo Generates

Expected output files include:

- `artifacts/demo/ledger.json`
- `artifacts/demo/audit/integrity.json`
- `artifacts/demo/audit/integrity.md`
- `artifacts/demo/reports/rw-snapshot-2025.json`
- `artifacts/demo/reports/rw-snapshot-2025.csv`
- `artifacts/demo/reports/rw-value-2025.json`
- `artifacts/demo/reports/rw-value-2025.csv`
- `artifacts/demo/reconciliation/reconciliation-summary.json`
- `artifacts/demo/reconciliation/reconciliation-summary.md`
- `artifacts/demo/config/italy-rw-2025.template.json`
- `artifacts/demo/config/italy-rw-2025.binance-filled.json`
- `artifacts/demo/accountant/italy-rw-accountant-2025.json`
- `artifacts/demo/accountant/italy-rw-accountant-2025.csv`
- `artifacts/demo/accountant/italy-rw-accountant-2025.md`
- `artifacts/demo/accountant/accountant-handoff-2025.json`
- `artifacts/demo/accountant/Reckonry-Tax-Dossier-2025.pdf`

`artifacts/` is ignored by Git. Generated ledgers, reports, PDFs, and private local outputs should not be committed.

## Inspect The Outputs

Start with:

```bash
cat artifacts/demo/audit/integrity.md
cat artifacts/demo/reconciliation/reconciliation-summary.md
cat artifacts/demo/accountant/italy-rw-accountant-2025.md
```

Open the Tax Dossier PDF from:

```text
artifacts/demo/accountant/Reckonry-Tax-Dossier-2025.pdf
```

The demo intentionally includes one unsupported Binance-style row. Reckonry should preserve it as an unknown event instead of discarding it.

## Demo Input Files

- `samples/demo/binance/normalized-transactions.csv`
- `samples/demo/official-reports/binance-italy-annual-balance-2025.pdf`
- `samples/demo/official-reports/binance-italy-tax-certification-2025.pdf`
- `samples/demo/italy-rw/italy-rw-2025.fake.json`
- `samples/demo/italy-rw/accountant-handoff-2025.fake.json`

All values are fake and synthetic.

## Limitations

- Reckonry is alpha-stage software.
- The demo proves one safe public workflow, not complete exchange coverage.
- The Binance importer is incomplete and preserves unsupported rows for review.
- The Italy RW and Tax Dossier outputs are professional review aids, not tax filings.
- Reckonry does not provide tax, legal, accounting, financial, or investment advice.
- Real exchange exports and generated private outputs belong in ignored local folders such as `input/`, `output/`, or `artifacts/`.
