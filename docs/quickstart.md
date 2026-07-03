# Quickstart

This quickstart runs the public Reckonry demo in about 10 minutes on a clean checkout.

The demo uses synthetic data only. It does not use real provider exports, wallet addresses, account identifiers, private keys, taxpayer data, or private financial records.

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

The demo uses the platform command shape:

```bash
reckonry plugins
reckonry import binance --input samples/demo/binance --out artifacts/demo/ledger.json
reckonry report integrity --input artifacts/demo/ledger.json --out artifacts/demo/audit
reckonry tax italy rw snapshot --input artifacts/demo/ledger.json --year 2025 --out artifacts/demo/reports
reckonry tax italy rw value --input artifacts/demo/ledger.json --year 2025 --out artifacts/demo/reports
reckonry reconcile binance italy --reports samples/demo/official-reports --ledger-reports artifacts/demo/reports --out artifacts/demo/reconciliation
reckonry tax italy dossier --year 2025 --ledger artifacts/demo/ledger.json --handoff artifacts/demo/accountant/accountant-handoff-2025.json --rw artifacts/demo/accountant/italy-rw-accountant-2025.json --out artifacts/demo/accountant --language en-US
```

Direct CLI commands may print privacy warnings when they read from tracked `samples/demo/` files or write inside the repository. The demo scripts suppress repeated repository-path warnings after stating that the inputs are synthetic. Real private data must stay in ignored local folders and must never be committed.

## Expected Command Output

The exact counts may change as the synthetic demo evolves, but a successful run
should include lines like:

```text
Reckonry public demo
Input data is synthetic and safe to commit publicly.
Expected alpha result: NOT READY FOR FILING. That means missing professional inputs are visible and not invented.
Installed source importers:
Imported ... event(s) using Binance CSV Importer.
Validation passed: ...
Wrote ledger integrity report to ...
Wrote RW snapshot report for 2025 to ...
Wrote RW value report for 2025 to ...
Wrote Binance Italy Reconciliation summary to ...
Generated accountant review package files:
Generated tax dossier:
Demo complete. Generated outputs:
What to inspect first:
```

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

## Troubleshooting

### `NETSDK1045` Or Unsupported Target Framework

Install a .NET SDK compatible with `net10.0`, then rerun:

```bash
dotnet --info
dotnet build Reckonry.sln
```

### `scripts/demo.sh: Permission denied`

Run the script through Bash:

```bash
bash scripts/demo.sh
```

### `Built CLI was not found`

Build the solution before running the demo:

```bash
dotnet build Reckonry.sln
scripts/demo.sh
```

### Repeated Privacy Warnings

The public demo intentionally reads tracked synthetic samples and writes ignored
outputs under `artifacts/demo/`. The warnings are expected for the demo. Do not
use tracked folders for real financial data.

### Missing Generated PDF

Confirm the previous demo commands completed without errors and inspect:

```bash
ls artifacts/demo/accountant
```

The expected PDF path is:

```text
artifacts/demo/accountant/Reckonry-Tax-Dossier-2025.pdf
```

## Demo Input Files

- `samples/demo/binance/normalized-transactions.csv`
- `samples/demo/official-reports/binance-italy-annual-balance-2025.pdf`
- `samples/demo/official-reports/binance-italy-tax-certification-2025.pdf`
- `samples/demo/italy-rw/italy-rw-2025.fake.json`
- `samples/demo/italy-rw/accountant-handoff-2025.fake.json`

All values are fake and synthetic.

## Limitations

- Reckonry is alpha-stage software.
- The demo proves one safe public workflow, not complete source/provider coverage.
- The Binance importer is incomplete and preserves unsupported rows for review.
- The Italy RW and Tax Dossier outputs are professional review aids, not tax filings.
- Reckonry does not provide tax, legal, accounting, financial, or investment advice.
- Real exchange exports and generated private outputs belong in ignored local folders such as `input/`, `output/`, or `artifacts/`.
