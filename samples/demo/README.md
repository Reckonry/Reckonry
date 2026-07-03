# Reckonry Public Demo Samples

This directory contains the public Reckonry demo input set.

Every value is synthetic. The files are safe to commit publicly and were created only to exercise the demo workflow.

## What Is Included

- `binance/normalized-transactions.csv`: fake Binance-style normalized transaction export.
- `official-reports/binance-italy-annual-balance-2025.pdf`: fake text PDF for reconciliation metadata extraction.
- `official-reports/binance-italy-tax-certification-2025.pdf`: fake text PDF for reconciliation metadata extraction.
- `italy-rw/italy-rw-2025.fake.json`: fake Italy RW configuration example for documentation and review.

## Safety Rules

- No real exchange exports are stored here.
- No real balances, wallet addresses, account identifiers, transaction IDs, private keys, API keys, taxpayer names, or tax records are stored here.
- Values are intentionally small, rounded, and synthetic.
- The demo includes one unsupported row so Reckonry can show how unknown source data is preserved.

Generated demo outputs belong under `artifacts/demo/`, which is ignored by Git.

