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
- Review `VERSIONING.md` for compatibility notes.
- Confirm release artifacts contain no private financial data.
- Confirm generated samples, logs, screenshots, and documentation use fake or anonymized data only.
- Confirm tax behavior is backed by official sources where applicable.
- Confirm ADRs are updated for architecture changes.
- Push a version tag only after the release branch is ready.

## Artifacts

The release workflow prepares build artifacts when a tag matching a prerelease format such as `v0.1.0-alpha` is pushed.

Reckonry does not publish NuGet packages yet. Release artifacts must not contain private financial data, real exchange exports, account identifiers, unredacted logs, or private tax configuration.
