# Reckonry Plugin Templates

This folder contains official `dotnet new` templates for Reckonry plugin
development.

Status: alpha, source-based templates.

Install locally from the repository root:

```bash
dotnet new install ./templates
```

Available templates:

- `reckonry-importer`
- `reckonry-tax-module`
- `reckonry-report`
- `reckonry-reconciliation`

The templates compile against the Reckonry source tree through MSBuild
`ProjectReference` entries. They are intended for local development before
stable external NuGet SDK packages exist.

