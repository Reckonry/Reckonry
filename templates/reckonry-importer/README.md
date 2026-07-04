# SampleImporter

Reckonry source importer plugin template.

This template shows the minimum shape of an importer:

- Implements `ISourceImporter`.
- Produces canonical `LedgerEvent` values.
- Preserves raw source rows through `SourceReference`.
- Emits `LedgerEventType.Unknown` for unsupported rows.
- Includes fake CSV data and tests.

## Build

From this template directory:

```bash
dotnet test tests/SampleImporter.Tests/SampleImporter.Tests.csproj
```

When generated outside the Reckonry repository, pass the Reckonry source path:

```bash
dotnet test tests/SampleImporter.Tests/SampleImporter.Tests.csproj -p:ReckonryRoot=/path/to/Reckonry
```

## Generate From Template

```bash
dotnet new reckonry-importer -n Contoso.Reckonry.Importers.Example
dotnet test Contoso.Reckonry.Importers.Example/tests/Contoso.Reckonry.Importers.Example.Tests/Contoso.Reckonry.Importers.Example.Tests.csproj -p:ReckonryRoot=/path/to/Reckonry
```

Do not use real exchange exports in this template. Keep fake data synthetic.

