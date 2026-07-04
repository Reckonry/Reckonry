# Showcase Screenshots

This directory contains product screenshots used by the README.

The screenshots must come from the real public demo workflow, not stock images,
conceptual artwork, or unrelated mockups.

## Source Workflow

Regenerate demo outputs first:

```bash
dotnet build Reckonry.sln
dotnet test Reckonry.sln
scripts/demo.sh
```

Screenshots should be rendered from generated files under `artifacts/demo/` and
then copied here only after privacy review.

## Privacy Rules

- Use synthetic public demo data only.
- Redact or blur financial values, hashes, names, addresses, account identifiers,
  wallet addresses, transaction hashes, and local absolute paths where
  appropriate.
- Keep layout, typography, product structure, and branding visible.
- Do not commit screenshots generated from real financial data.

## Current Screenshots

| File | Represents |
| --- | --- |
| `cli.png` | Demo CLI workflow output. |
| `tax-dossier.png` | Tax Dossier PDF generated from the synthetic demo. |
| `audit.png` | Ledger integrity report generated from the synthetic demo. |
| `accountant.png` | Italy RW accountant package generated from the synthetic demo. |
| `rw.png` | RW snapshot and value report structures generated from the synthetic demo. |

