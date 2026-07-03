# Security Policy

Reckonry handles workflows that may involve sensitive financial records, exchange exports, ledger artifacts, tax configuration, and generated reports. Security and privacy issues are treated as release blockers when they can expose private data or weaken auditability.

## Supported Versions

Reckonry is pre-1.0 software. Security fixes target the active development branch unless a release line is explicitly marked as supported.

## Reporting A Vulnerability

Do not open public issues for suspected vulnerabilities.

Use GitHub private vulnerability reporting for this repository:

```text
https://github.com/Reckonry/Reckonry/security/advisories/new
```

If GitHub private vulnerability reporting is unavailable, contact the maintainer
through the least public available GitHub path first and ask for a private
disclosure channel. Do not include private financial data in the first message.

Include:

- A description of the issue.
- Steps to reproduce using fake or anonymized data.
- Potential impact.
- Affected version, commit, or branch.
- Any suggested mitigation.

## Sensitive Data

Do not send real exchange exports, real ledgers, account identifiers, private tax configuration, API keys, seed phrases, unredacted logs, or generated reports containing private financial data.

## Security Review Priorities

Changes involving importers, file parsing, generated artifacts, release packaging, dependency updates, or privacy boundaries require careful review.
