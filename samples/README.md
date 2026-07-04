# Samples

Files under `samples/` must be fake, anonymized, or synthetic examples only.

Do not place real exchange exports, account identifiers, transaction IDs, wallet addresses, balances, API keys, tax records, or personally identifying information in this directory.

Use local ignored folders for private data:

- `input/` for real exchange exports.
- `output/` for generated ledgers and reports.

Never commit real CSV exports, `ledger.json`, generated reports, or private financial data.

## Directory Roles

- `samples/demo/` is the canonical public end-to-end demo input set used by the
  quickstart and README. It currently includes synthetic Binance and Coinbase
  source inputs.
- `samples/binance/` contains small importer-development fixtures for Binance
  parser behavior. These are also fake/synthetic and are not the public
  end-to-end demo workflow.
