# Governance

Reckonry is currently founder-led. Until the contributor community grows enough to support a broader governance body, final project direction and release decisions are made by the founder and maintainer.

## Maintainer Model

- Reckonry uses a BDFL / founder-led model during the early project phase.
- The maintainer is responsible for release readiness, project scope, repository standards, and final review decisions.
- Maintainer authority is expected to become more distributed as trusted contributors emerge.

## Architecture Decisions

Architecture decisions are documented through ADRs in [docs/adr](docs/adr/README.md). Changes that affect module boundaries, ledger schema compatibility, plugin contracts, tax module boundaries, persistence, or release compatibility should include an ADR update.

## Review Priorities

Security, privacy, and financial-data integrity take priority over feature speed.

Pull requests are reviewed for:

- Source preservation and auditability.
- No invented financial values.
- No real financial data in tests, samples, screenshots, logs, or generated artifacts.
- Decimal arithmetic for financial and digital asset amounts.
- Compatibility with the canonical ledger model.
- Clear official sources for country-specific tax behavior.

## Tax Logic

Professional tax logic requires official sources. Informal summaries, blog posts, social media posts, or unsupported assumptions are not enough for implementation.

Tax modules may interpret the canonical ledger, but they must not mutate it. Country-specific behavior must stay outside `Reckonry.Core`.

## Security-First Review

Changes that affect file handling, importer parsing, report generation, artifact packaging, dependency behavior, or privacy boundaries require security and privacy review before release.
