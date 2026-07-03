# Public Readiness Roadmap

This roadmap answers one question:

> What must be true before Reckonry deserves to be recommended?

Reckonry should not become public because the repository exists. It should become public when a serious developer, technical accountant, auditor, or crypto power user can run it, inspect it, understand its limits, and decide whether the output is trustworthy enough for professional review.

Public readiness does not mean Reckonry is finished. For `v0.1.0-alpha`, it means the project is honest, reproducible, useful for one real workflow, and careful about financial data.

## 1. Product Positioning

Reckonry is not a tax calculator.

Reckonry is verifiable financial ledger infrastructure for reconstructing digital asset activity from imperfect source data.

The core promise is:

```text
Build. Verify. Trust.
```

The project should be positioned around these claims:

- Build a canonical ledger from fragmented source exports.
- Verify what happened through hashes, source references, validation, audit checks, and reconciliation.
- Trust the process because every generated number is explainable.
- Preserve uncertainty instead of hiding it.
- Never invent financial data.

Reckonry should avoid claims such as:

- "Calculate your taxes automatically."
- "Guarantee correct tax filings."
- "Replace your accountant."
- "Support every exchange."
- "Produce complete capital gains reports for every country."

The honest public message is stricter:

Reckonry reconstructs and verifies financial ledgers. Tax modules and reports are professional review aids, not legal, tax, accounting, or financial advice.

## 2. Target Users

## Developers

Developers need clean architecture, stable contracts, repeatable tests, fake sample data, and clear extension points. They should be able to add an importer, report, reconciliation module, or tax module without reading private maintainer context.

## Technical Accountants

Technical accountants need source traceability, deterministic reports, explicit warnings, reproducible artifacts, and clean handoff material. They should be able to inspect where numbers came from and identify missing inputs.

## Auditors

Auditors need evidence trails, immutable ledger semantics, hashes, source references, changelogs, release notes, and clear distinction between facts and interpretations.

## Crypto Power Users

Power users need a local-first tool that can ingest exchange data, produce a ledger, surface unknowns, and generate a review package without uploading private records to a hosted service.

## Future Non-Technical Users

Future non-technical users need a guided UX, safer defaults, clearer diagnostics, and less command-line exposure. They are not the first launch audience. The alpha should not pretend to be ready for them.

## 3. Minimum Public Launch Criteria

Before the repository becomes public, the following must be true:

- The README is clean, current, and honest about alpha status.
- Branding is strong enough to communicate the project quickly: name, promise, principles, screenshots, and examples.
- The CLI works for the public demo path on a clean machine.
- A reproducible sample workflow exists from fake source data to generated outputs.
- Fake sample data is committed and clearly marked as fake.
- No private data exists in the repository history, samples, docs, screenshots, tests, logs, or generated artifacts.
- Build and test GitHub Actions pass on pull requests.
- Contribution docs exist and explain privacy, testing, ADRs, and tax-source requirements.
- Security policy exists and clearly says not to disclose private financial data publicly.
- License and commercial licensing posture are clear.
- Roadmap exists and does not overpromise.
- Public examples exist for CLI commands, input fixtures, generated ledger, generated audit output, and generated Tax Dossier.
- Screenshots or generated PDF examples are available and safe to publish.
- Known limitations are documented near the quickstart, not hidden deep in the repo.

If any of these are missing, the repo can be shared privately, but it should not be broadly recommended.

## 4. Developer Onboarding: First 10 Minutes

The first 10-minute experience should be deterministic and boring:

1. Clone the repository.
2. Confirm the required .NET SDK version.
3. Run `dotnet build Reckonry.sln`.
4. Run `dotnet test Reckonry.sln`.
5. Run a sample Binance import using committed fake data.
6. Generate a sample `ledger.json`.
7. Generate a sample audit package.
8. Generate a sample Tax Dossier PDF.
9. Inspect generated output under a known local folder.
10. Compare output against committed expected examples or documented screenshots.

This path should not require secrets, database setup, private files, paid services, network calls, or real financial data.

The target result:

A new developer can decide within 10 minutes whether Reckonry is real, testable, and worth deeper evaluation.

## 5. Sample And Demo Mode

The public demo must be full enough to show the product workflow, but fake enough to be safe forever.

## Demo Scenario

The demo should include:

- A fake Binance export with realistic columns and synthetic activity.
- A fake official report used for reconciliation.
- A fake Italy RW configuration file.
- A generated sample Tax Dossier PDF.
- A sample audit package.
- A generated canonical ledger.
- Expected warnings for incomplete or unsupported activity.

## Demo Rules

- All names, account identifiers, transaction IDs, wallet addresses, timestamps, amounts, prices, fees, and balances must be synthetic.
- No row should be copied from a real export unless every sensitive and account-specific value is replaced.
- Demo data should include enough complexity to exercise the workflow:
  - deposits
  - withdrawals
  - trades
  - fees
  - conversions
  - rewards or staking
  - unsupported rows
  - reconciliation differences
- The demo should include at least one unknown row to prove Reckonry preserves uncertainty.
- The generated PDF and audit outputs must be safe to commit publicly.

## Demo Command Shape

The final demo should be runnable with commands similar to:

```bash
dotnet run --project src/Reckonry.Cli/Reckonry.Cli.csproj -- import binance --input ./samples/demo/binance --out ./artifacts/demo/ledger.json
dotnet run --project src/Reckonry.Cli/Reckonry.Cli.csproj -- audit --input ./artifacts/demo/ledger.json --out ./artifacts/demo/audit
dotnet run --project src/Reckonry.Cli/Reckonry.Cli.csproj -- report tax-dossier --year 2025 --ledger ./artifacts/demo/ledger.json --out ./artifacts/demo/tax-dossier
```

The exact commands can change, but the public experience must remain one clear path.

## 6. Documentation Required

The public docs should include:

- Quickstart: build, test, run the demo, inspect output.
- CLI reference: commands, options, exit codes, examples.
- Architecture: module boundaries, ledger immutability, source references.
- Importer guide: how importers map source data and preserve unknown rows.
- Tax module guide: official-source requirement, country isolation, no ledger mutation.
- Reconciliation guide: compare external reports without replacing the ledger.
- Privacy guide: local-first workflow, ignored folders, safe sample rules.
- Security guide: vulnerability reporting and sensitive data rules.
- FAQ: what Reckonry is and is not.
- Limitations: alpha status, supported importers, unsupported workflows, known gaps.
- Disclaimer: not tax, legal, accounting, financial, or investment advice.
- Professional review guide: how accountants and auditors should inspect evidence, warnings, and generated outputs.

The docs should use the same vocabulary everywhere:

- canonical ledger
- source evidence
- audit package
- reconciliation
- Tax Dossier
- professional review
- no invented financial data

## 7. Trust Signals

Reckonry becomes trustworthy through observable engineering behavior, not claims.

Required trust signals:

- Tests cover ledger validation, importer behavior, unknown row preservation, reports, audit checks, and tax module boundaries.
- Outputs are deterministic for the same inputs and options.
- SHA256 hashes are included where they help verify files, artifacts, or source evidence.
- No telemetry exists by default.
- Processing is local-first.
- There are no hidden network calls in the default CLI workflow.
- Tax logic is backed by official sources and linked from docs or tests.
- ADRs explain architecture decisions and compatibility-sensitive choices.
- Changelog records user-visible and compatibility-relevant changes.
- Release process explains tags, artifacts, limitations, and data-safety requirements.
- CI proves the solution builds and tests from a clean checkout.

Trust also requires restraint:

- Do not hide unsupported data.
- Do not estimate missing prices by default.
- Do not turn warnings into silent defaults.
- Do not imply legal or tax certainty.

## 8. Feature Gaps Before Public Alpha

The following gaps should be closed before `v0.1.0-alpha` is recommended publicly:

- Better fake samples that exercise the full public workflow.
- A generated sample Tax Dossier PDF that is safe to publish.
- Configuration templates for demo and real local workflows.
- GitHub Actions for build and test.
- Issue templates for bugs, features, importers, and country tax requests.
- Release packaging workflow that prepares artifacts without publishing NuGet packages prematurely.
- NuGet packaging decision: defer, publish preview packages, or keep source-only for alpha.
- Binary release decision: provide CLI binaries, source-only instructions, or both.
- Documentation polish for quickstart, CLI, architecture, privacy, and limitations.
- Branding polish: screenshots, generated PDF examples, badges, and consistent terminology.
- Public examples that can be regenerated and compared.
- Repository scan for accidental private data before launch.

Some gaps can remain after public alpha if they are explicit. Hidden gaps are the problem.

## 9. Future UX Path

## Phase 1: CLI First

The CLI is the correct first public surface. It is reproducible, scriptable, testable, and appropriate for developers, accountants with technical workflows, auditors, and power users.

CLI readiness requires:

- clear commands
- stable sample workflow
- useful validation errors
- deterministic outputs
- local-first behavior

## Phase 2: Desktop App Later

A desktop app can make Reckonry usable for less technical users while preserving local-first privacy. It should come after the CLI workflow is stable enough to wrap.

Desktop readiness requires:

- safe file selection
- guided output inspection
- clear warnings
- no accidental data upload
- exportable audit packages

## Phase 3: Web UI Later

A local web UI can help inspect ledgers, reports, and reconciliation results. It should not become the first public promise unless the underlying CLI and data model are stable.

## Phase 4: Hosted SaaS Only With A Clear Privacy Model

Hosted SaaS should be considered only if the privacy model is explicit, defensible, and optional. Reckonry's default trust story is local-first. A hosted model changes the risk profile and must not be introduced casually.

## 10. Recommendation-Worthiness Checklist

Reckonry can be recommended when:

- It solves a real workflow end-to-end.
- It is documented well enough for a new user to run without maintainer help.
- It is reproducible from fake sample data.
- It is clear about limitations.
- It does not claim legal or tax certainty.
- It provides professional-review outputs.
- Users can validate results back to source evidence.
- Build and test automation pass publicly.
- The demo works on a clean machine.
- Public artifacts contain no private data.
- The project explains what it will not do yet.

Recommendation should be scoped:

- Recommended for developers evaluating ledger reconstruction: yes, after public alpha criteria are met.
- Recommended for accountants and auditors as a review aid: yes, only with clear limitations and fake demo evidence.
- Recommended as a tax filing product: no.
- Recommended for non-technical users: not during alpha.

## 11. Outreach Plan

Launch channels should be chosen for serious feedback, not vanity traffic.

- GitHub: public repository, releases, issues, discussions if enabled.
- Hacker News: technical launch post focused on ledger reconstruction, evidence, and local-first processing.
- Reddit dotnet: architecture, .NET implementation, CLI, and testing angle.
- Reddit opensource: governance, roadmap, contribution opportunities.
- Reddit crypto tax/accounting communities: careful positioning as review infrastructure, not tax advice.
- LinkedIn technical post: accountant/auditor/developer framing.
- Blog post: "Why crypto accounting needs a verifiable ledger layer."
- Demo video: 5 to 8 minutes, fake data only, from import to Tax Dossier.
- Italian developer/accountant communities: Italy RW review workflow, official sources, professional review framing.

Outreach should link to limitations and disclaimers directly. Do not route interested users into a product claim stronger than the software can support.

## 12. First Public Release

## Version

The first public release should be:

```text
v0.1.0-alpha
```

## Included

- Canonical ledger foundation.
- Working CLI for the public demo workflow.
- Binance importer foundation for supported fake sample exports.
- Unknown row preservation.
- Validation and audit package generation.
- Italy RW accountant-oriented output where implemented and source-backed.
- Tax Dossier sample output for professional review.
- Fake demo data.
- Build and test GitHub Actions.
- Governance, contribution, security, support, versioning, release, roadmap, and changelog docs.

## Explicitly Not Included

- Tax filing guarantees.
- Legal, tax, accounting, financial, or investment advice.
- Complete exchange coverage.
- Complete wallet coverage.
- Hosted processing.
- Production-ready non-technical UX.
- NuGet publishing unless a deliberate release decision is made.
- Guaranteed stable CLI or schema compatibility beyond documented alpha expectations.

## Release Notes Outline

Release notes should include:

- What Reckonry is.
- What `v0.1.0-alpha` can do.
- What it cannot do.
- Supported demo workflow.
- Supported importer scope.
- Known limitations.
- Privacy warning.
- Verification commands.
- Artifact list.
- Contribution paths.
- Links to official-source-backed tax docs where relevant.

## Artifact List

Potential artifacts:

- Source archive generated by GitHub.
- CLI build artifact if binary release is approved.
- Generated fake sample ledger.
- Generated fake sample audit package.
- Generated fake sample Tax Dossier PDF.
- Checksums for generated artifacts.

Artifacts must not contain private data.

## Known Limitations

Known limitations should include:

- Alpha status.
- Limited importer coverage.
- Limited tax module scope.
- Possible schema changes before stable release.
- No hosted service.
- No guarantee of tax correctness.
- Fake sample workflow is not proof of complete real-world coverage.
- Professional review required before relying on outputs.

## 13. Success Metrics

Early success should measure useful engagement, not only attention.

- Stars: signal that the positioning resonates.
- Issues: signal that users are trying the project.
- First external user: someone outside the maintainer's environment runs the demo.
- First importer request: evidence that users understand the extension model.
- First accountant feedback: validation from the professional review audience.
- First contributor: someone opens a PR that follows project rules.
- First bug report with sample data: a reproducible report using fake or anonymized data.

Better metrics after alpha:

- Percentage of issues with reproducible fake samples.
- Number of importer schemas documented.
- Number of outputs with deterministic snapshot tests.
- Number of official tax sources linked to tax module behavior.
- Time for a new developer to complete the quickstart.

## Prioritized Action List

1. Complete the public fake demo workflow end-to-end.
2. Generate and commit safe sample outputs: ledger, audit package, and Tax Dossier PDF.
3. Write the quickstart around the demo workflow.
4. Polish README positioning and limitations.
5. Confirm build and test GitHub Actions pass.
6. Add or polish CLI reference and professional review guide.
7. Review repository for private data and unsafe samples.
8. Decide release artifact policy for `v0.1.0-alpha`.
9. Create release notes draft.
10. Record remaining limitations explicitly before launch.

## Codex Task Breakdown

Use small implementation tasks with clear verification:

1. Audit current docs and produce a missing-docs checklist.
2. Build `samples/demo` with fake Binance exports, fake official report, and fake Italy RW config.
3. Add a demo command script or documented command sequence.
4. Generate sample ledger output from fake data.
5. Generate sample audit package from fake data.
6. Generate sample Tax Dossier PDF from fake data.
7. Add checksums for public sample outputs.
8. Write `docs/quickstart.md`.
9. Write or update CLI reference docs.
10. Write professional review guide.
11. Add limitations and FAQ docs.
12. Review README for public launch clarity.
13. Verify CI workflows on a clean branch.
14. Draft `v0.1.0-alpha` release notes.
15. Run a private data audit before making the repository public.

Each Codex task should report changed files, commands run, and whether any product code or tax logic changed.

## Launch Checklist

- [ ] README is clear, accurate, and alpha-scoped.
- [ ] Quickstart works on a clean checkout.
- [ ] `dotnet build Reckonry.sln` passes.
- [ ] `dotnet test Reckonry.sln` passes.
- [ ] Demo import works with fake data.
- [ ] Demo ledger is generated.
- [ ] Demo audit package is generated.
- [ ] Demo Tax Dossier PDF is generated.
- [ ] Public sample files are fake or anonymized.
- [ ] No private data appears in tracked files.
- [ ] Build and test GitHub Actions pass.
- [ ] Issue templates exist.
- [ ] PR template exists.
- [ ] Contribution docs exist.
- [ ] Security policy exists.
- [ ] License and commercial licensing posture are clear.
- [ ] Roadmap is public and practical.
- [ ] Limitations are documented.
- [ ] Release process is documented.
- [ ] `v0.1.0-alpha` release notes are drafted.
- [ ] Outreach copy avoids tax calculator claims.
- [ ] Maintainer has decided whether binaries are included.
- [ ] Maintainer has decided whether NuGet publishing is deferred.

Reckonry deserves recommendation only when a user can verify the workflow, inspect the evidence, understand the limitations, and leave with more clarity than they started with.
