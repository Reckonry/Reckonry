# Release Process

Reckonry uses semantic versioning and staged release maturity.

## Version Stages

- Alpha: early releases for validation, feedback, and integration testing. Breaking changes are expected.
- Beta: feature-complete candidates for a release line. Breaking changes should be rare and documented.
- Stable: releases with compatibility expectations described in [VERSIONING.md](VERSIONING.md).

## Tag Format

Release tags use a leading `v`:

```text
v0.1.0-alpha
v0.2.0-beta.1
v1.0.0
```

## Release Checklist

- Confirm `dotnet build Reckonry.sln` passes.
- Confirm `dotnet test Reckonry.sln` passes.
- Review `CHANGELOG.md`.
- Draft release notes under `docs/releases/` for the exact version being released.
- Review `VERSIONING.md` for compatibility notes.
- Confirm release artifacts contain no private financial data.
- Confirm generated samples, logs, screenshots, and documentation use fake or anonymized data only.
- Confirm tax behavior is backed by official sources where applicable.
- Confirm ADRs are updated for architecture changes.
- Generate and review SHA256 checksums for every release artifact.
- Push a version tag only after the release branch is ready.

## Artifacts

The release workflow prepares build artifacts and SHA256 checksum files when a tag matching a prerelease format such as `v0.1.0-alpha` is pushed.

Expected public alpha artifact names:

```text
reckonry-cli-v0.1.0-alpha.tar.gz
reckonry-cli-v0.1.0-alpha.tar.gz.sha256
```

Verify a downloaded artifact with:

```bash
sha256sum -c reckonry-cli-v0.1.0-alpha.tar.gz.sha256
```

Reckonry does not publish NuGet packages yet. Release artifacts must not contain private financial data, real exchange exports, account identifiers, unredacted logs, or private tax configuration.

## Public Alpha Scope

`v0.1.0-alpha` is CLI-first. `Reckonry.Api` is an experimental in-memory host
for architecture validation and descriptor inspection. It is not part of the
supported public alpha workflow, is not a deployable product API, and is not a
stable public contract.
