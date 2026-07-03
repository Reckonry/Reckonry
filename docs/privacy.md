# Privacy And Local Data Safety

LedgerForge is designed to process sensitive financial data locally. Real exchange exports, generated ledgers, and reports can contain private balances, transaction history, account identifiers, wallet addresses, and other financial information.

## Safe Local Workflow

1. Put real Binance exports in `input/`.
2. Run import:

   ```bash
   ledgerforge import binance --input ./input/binance --out ./output/ledger.json
   ```

3. Write reports to `output/`:

   ```bash
   ledgerforge report rw-snapshot --input ./output/ledger.json --year 2025 --out ./output/reports
   ```

4. Keep private Italy RW configuration under `input/italy-rw/`:

   ```bash
   ledgerforge config italy-rw-template --year 2025 --ledger ./output/ledger.json --out ./input/italy-rw/italy-rw-2025.json
   ```

5. Never commit real CSV, `ledger.json`, reports, accountant workpapers, or private RW configuration.

## Git Safety

The repository ignores `input/` and `output/` by default. Keep private files there when working locally.

Do not put real exports under tracked folders such as `samples/`, `docs/`, `src/`, or `tests/`. The CLI warns when an input path appears to be inside the repository outside ignored local data folders, but the warning is not a substitute for reviewing `git status` before every commit.

## Sample Data

Sample files must be fake, anonymized, or synthetic. If a sample row came from a real export, remove or replace every account-specific value before adding it to the repository.
