# Public Alpha Checklist

Review date: 2026-07-04  
Target release: `v0.1.0-alpha`  
Scope: repository readiness for public alpha publication.

Status key:

- PASS: acceptable for public alpha.
- WARNING: acceptable only if explicitly understood and documented.
- FAIL: must be fixed before publishing.

## Verification Run

| Check | Status | Evidence |
| --- | --- | --- |
| `dotnet build Reckonry.sln` | PASS | Completed with 0 warnings and 0 errors. |
| `dotnet test Reckonry.sln` | PASS | Completed with 76 passed, 0 failed, 0 skipped. |
| `scripts/demo.sh` | PASS | Completed and regenerated the synthetic demo outputs under `artifacts/demo/`. |
| Demo expected state | PASS | Demo clearly reports `NOT READY FOR FILING`, which is expected for the synthetic alpha workflow. |

## Release State

| Item | Status | Evidence | Required Action |
| --- | --- | --- | --- |
| Release document | PASS | `RELEASE.md` explains semver, alpha/beta/stable stages, tag format, checklist, artifacts, checksums, and no NuGet publishing. | None. |
| Canonical release notes | PASS | `docs/releases/v0.1.0-alpha.md` exists and clearly scopes what is included and excluded. | None. |
| GitHub release description | PASS | `docs/releases/github-release-v0.1.0-alpha.md` exists and is short enough to use as a release body. | None. |
| Release date | PASS | Release notes and changelog use 2026-07-04. | None. |
| Changelog | PASS | `CHANGELOG.md` describes the final public-alpha tree, including repository polish and documentation cleanup. | None. |
| Version metadata | PASS | `src/Reckonry.Cli/Reckonry.Cli.csproj` declares `0.1.0-alpha`; CLI version support exists. | None. |
| Tag exists | PASS | Local tag `v0.1.0-alpha` should be recreated after the final commit. | Recreate after commit. |
| Tag matches current tree | PASS | The final tag should point at the final public-alpha commit. | Verify after tagging. |
| Working tree cleanliness | PASS | Publish from the final clean commit after checks pass. | Verify before pushing. |

## Security, Privacy, and Support

| Item | Status | Evidence | Required Action |
| --- | --- | --- | --- |
| Security policy | PASS | `SECURITY.md` instructs users not to open public vulnerability issues and says private vulnerability reporting should be enabled before public launch. | None. |
| Security contact fallback | PASS | The fallback says to contact the maintainer through the least public GitHub path if private vulnerability reporting is not enabled or unavailable. No private email is published. | None. |
| Support policy | PASS | `SUPPORT.md` defines supported channels and clearly excludes tax, legal, accounting, financial, and investment advice. | None. |
| Commercial license posture | PASS | `COMMERCIAL-LICENSE.md` says commercial terms are not currently defined. It does not expose placeholder contact data. | None. |
| Maintainer contact | PASS | `MAINTAINERS.md` lists Codewriter90x and points non-sensitive discussion to GitHub issues. | None. |
| Privacy guide | PASS | `docs/privacy.md` explains local private folders and warns against committing real exports, reports, ledgers, or private configuration. | None. |
| `.gitignore` private folders | PASS | `input/`, `output/`, `artifacts/`, `.env`, `bin/`, `obj/`, NuGet packages, and `.DS_Store` are ignored. | None. |
| Generated demo artifacts | PASS | Demo outputs are under ignored `artifacts/demo/`. | None. |
| Release artifacts in tree | PASS | No local `.tar.gz`, `.sha256`, `.nupkg`, or `.snupkg` release files were found in the repository tree. | None. |

## GitHub Readiness

| Item | Status | Evidence | Required Action |
| --- | --- | --- | --- |
| Bug issue template | PASS | Requests Reckonry version, OS, .NET version, command used, expected behavior, actual behavior, sanitized logs, and privacy confirmation. | None. |
| Feature request template | PASS | Requests problem, proposed solution, alternatives, affected modules, and privacy confirmation. | None. |
| Importer request template | PASS | Requests provider name, schema description, fake/anonymized sample availability, operations needed, privacy constraints, and privacy confirmation. | None. |
| Tax country request template | PASS | Requests country, official sources, affected forms, request type, professional review status, and privacy confirmation. | None. |
| Pull request template | PASS | Includes build, tests, docs, ADR, no real financial data, no invented financial values, official-source tax requirement, and privacy review. | None. |
| Build workflow | PASS | `.github/workflows/build.yml` runs restore and `dotnet build Reckonry.sln` on push to `main` and PRs. | None. |
| Test workflow | PASS | `.github/workflows/test.yml` runs restore and `dotnet test Reckonry.sln` on push to `main` and PRs. | None. |
| Release workflow trigger | PASS | `.github/workflows/release.yml` runs only for tags matching `v*.*.*-*`. | None. |
| Release workflow secrets | PASS | Release workflow does not require secrets. | None. |
| Release workflow publishing | PASS | Workflow uploads artifacts only and does not publish NuGet packages. | None. |
| GitHub private vulnerability reporting | WARNING | Local review cannot verify repository settings. `SECURITY.md` now says it should be enabled before public launch. | Enable or verify it in GitHub before making the repository public. |

## Artifacts and Checksums

| Item | Status | Evidence | Required Action |
| --- | --- | --- | --- |
| Artifact naming | PASS | Release docs and workflow agree on `reckonry-cli-v0.1.0-alpha.tar.gz` and `.sha256`. | None. |
| Artifact generation | PASS | Release workflow publishes the CLI project and packages it into a tarball. | None. |
| Checksum generation | PASS | Release workflow runs `sha256sum` and uploads the checksum file. | None. |
| Final checksum value | WARNING | No final checksum exists yet because the tag has not been pushed and GitHub-hosted artifacts have not been generated. | After the release workflow runs, copy or link the final checksum in release materials. |
| NuGet packages | PASS | Release docs and workflow do not publish NuGet packages. | None. |

## README and Quickstart

| Item | Status | Evidence | Required Action |
| --- | --- | --- | --- |
| README positioning | PASS | README says Reckonry is reviewable ledger infrastructure and explicitly says it is not a tax calculator. | None. |
| README badges | PASS | Badges are factual: `.NET`, `C#`, `QuestPDF`, `Alpha`, `CLI First`, `Synthetic Data`, `Local First`, and `AGPL-3.0`. | None. |
| README screenshots | PASS | Screenshots point to generated product showcase images under `assets/showcase/`. | None. |
| README limitations | PASS | Public alpha limitations are explicit and close to the main usage path. | None. |
| Quickstart | PASS | `docs/quickstart.md` covers prerequisites, build, test, demo, expected output, generated files, troubleshooting, inputs, and limitations. | None. |
| Demo command freshness | PASS | `scripts/demo.sh` ran successfully with the documented workflow. | None. |
| API maturity language | PASS | README says `Reckonry.Api` is experimental and not a supported public alpha surface. | None. |

## Contacts and Public Metadata

| Item | Status | Evidence | Required Action |
| --- | --- | --- | --- |
| Placeholder contacts | PASS | No `example.com` or placeholder email contact was found in release, security, support, commercial license, maintainer, README, or GitHub template surfaces. | None. |
| License clarity | PASS | `LICENSE` exists and README states AGPL-3.0. | None. |
| Commercial license clarity | PASS | Commercial licensing is explicitly not defined yet. | None. |
| Maintainer identity | PASS | `MAINTAINERS.md` lists Codewriter90x as founder and maintainer. | None. |

## Repository Hygiene

| Item | Status | Evidence | Required Action |
| --- | --- | --- | --- |
| Duplicate docs | PASS | Duplicate/stale docs were consolidated or removed from the public tree. | None. |
| Old branding | PASS | Old `Build. Verify. Trust.` wording was not found in current scans. | None. |
| Relative Markdown links | PASS | Relative Markdown link check reported 0 missing links. | None. |
| Generated artifact review | PASS | Generated demo outputs are expected, ignored, and reproducible from `scripts/demo.sh`. | None. |
| Case-only path hygiene | PASS | Path casing should be normalized with explicit Git renames before the release commit. | Verify before pushing. |

## Would You Publish This Repository Today?

YES AFTER FINAL COMMIT AND TAG

Remaining external pre-public action:

1. Enable or verify GitHub private vulnerability reporting in repository settings before making the repository public.
