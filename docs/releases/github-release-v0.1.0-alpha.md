# Reckonry v0.1.0-alpha

Reckonry `v0.1.0-alpha` is the first public alpha of Reckonry: open-source infrastructure for reconstructing, validating, auditing, reconciling, and reviewing digital asset ledger evidence.

This is alpha software. It is CLI-first, local-first, and intentionally scoped. Reckonry is not a tax calculator, filing product, accounting system, hosted service, or source of legal/tax certainty.

## Included

- Canonical ledger v1 foundations.
- Binance normalized CSV importer demo workflow.
- Ledger validation.
- Audit, integrity, ledger, and summary reports.
- Binance Italy reconciliation against synthetic official-report inputs.
- Italy RW professional-review outputs:
  - RW snapshot report.
  - RW value report.
  - Accountant handoff package.
  - Accountant package in Markdown, CSV, and JSON.
- Tax Dossier PDF for professional review.
- Bundled plugin discovery for importers, country tax modules, reports, reconciliation modules, and pricing providers.
- Synthetic public demo data under `samples/demo/`.
- GitHub governance, issue templates, PR template, and build/test/release workflows.
- Local-first privacy posture with ignored private folders and no telemetry.

## Not Included

- Tax filing.
- Tax certainty.
- Legal, tax, accounting, financial, or investment advice.
- Production API support.
- Hosted SaaS workflows.
- Authentication or database persistence.
- NuGet package publishing.
- Stable external SDK or plugin compatibility guarantees.
- Complete Binance export coverage.
- Real Coinbase, Kraken, Revolut, Crypto.com, or Bitstamp importers.
- Automatic market pricing.
- Automatic completion of missing valuation evidence.

## Run The Demo

macOS/Linux:

```bash
dotnet build Reckonry.sln
dotnet test Reckonry.sln
scripts/demo.sh
```

Windows PowerShell:

```powershell
dotnet build Reckonry.sln
dotnet test Reckonry.sln
./scripts/demo.ps1
```

The demo uses synthetic data only and writes generated outputs to ignored local files under `artifacts/demo/`.

Expected alpha result: `NOT READY FOR FILING`.

That is intentional. The demo shows that missing professional inputs are visible and are not invented.

## Privacy Notes

- Do not commit private exchange exports, real ledgers, taxpayer data, wallet addresses, transaction hashes, account identifiers, API keys, seed phrases, passwords, or generated reports based on private data.
- `input/`, `output/`, `artifacts/`, `.env`, `bin/`, `obj/`, and `.DS_Store` are ignored.
- Public samples are synthetic.
- The demo does not require secrets, telemetry, hosted services, databases, or network calls after dependencies are restored.

## Release Artifacts

The GitHub release workflow prepares:

- `reckonry-cli-v0.1.0-alpha.tar.gz`
- `reckonry-cli-v0.1.0-alpha.tar.gz.sha256`

No NuGet packages are published for this release.

## Known Limitations

- This is alpha software.
- CLI behavior, schemas, report layouts, and extension contracts may change before `v1.0.0`.
- The CLI is the supported public alpha surface.
- `Reckonry.Api` is experimental and not part of the supported public alpha workflow.
- Placeholder importer projects are discoverable but do not parse real provider files.
- The current public demo proves one complete provider/country workflow only: Binance Italy with synthetic data.
- Italy RW outputs and the Tax Dossier are professional-review artifacts, not filing documents.

## Professional Review Disclaimer

Reckonry does not determine tax liability, calculate taxes, provide filing advice, or replace professional judgment.

Any tax-adjacent output must be reviewed by a qualified professional against current official sources and complete taxpayer evidence before it is used for accounting, audit, tax, legal, financial, or investment decisions.

Missing values remain missing. Reckonry must not invent financial data.

