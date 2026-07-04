# Snapshot Update Workflow

These snapshots protect public alpha behavior and generated artifact structure.

Update snapshots only after intentionally reviewing user-facing output changes:

```bash
RECKONRY_UPDATE_SNAPSHOTS=1 dotnet test Reckonry.sln --filter PublicArtifactSnapshotTests
dotnet test Reckonry.sln
```

Snapshots must be generated only from synthetic demo data. Do not paste or
commit private financial data, real exchange exports, private generated
reports, wallet addresses, account identifiers, or unredacted local paths.
