# Plugin Templates

Status: Official alpha templates, source-based SDK.

Reckonry includes `dotnet new` templates for building modules against the
current internal contracts:

- `reckonry-importer`
- `reckonry-tax-module`
- `reckonry-report`
- `reckonry-reconciliation`

These templates are intended to make external development easier before stable
NuGet SDK packages exist. They compile against a local Reckonry source checkout
through MSBuild `ProjectReference` entries.

## Install Templates

From the Reckonry repository root:

```bash
dotnet new install ./templates
```

List installed Reckonry templates:

```bash
dotnet new list reckonry
```

## Template Commands

```bash
dotnet new reckonry-importer -n Contoso.Reckonry.Importers.Example
dotnet new reckonry-tax-module -n Contoso.Reckonry.Tax.Example
dotnet new reckonry-report -n Contoso.Reckonry.Reports.Example
dotnet new reckonry-reconciliation -n Contoso.Reckonry.Reconciliation.Example
```

If the generated project is outside the Reckonry repository, pass the source
checkout path when building or testing:

```bash
dotnet test Contoso.Reckonry.Importers.Example/tests/Contoso.Reckonry.Importers.Example.Tests/Contoso.Reckonry.Importers.Example.Tests.csproj -p:ReckonryRoot=/path/to/Reckonry
```

## Template Contents

Each template contains:

- a compiling source project
- an xUnit test project
- fake or synthetic sample data
- `README.md`
- `ARCHITECTURE.md`
- current Reckonry contract usage

No template includes real financial data, real exchange exports, wallet
addresses, transaction hashes, tax identifiers, or private records.

## Build Your First Importer

This tutorial creates a source importer from the official template.

### 1. Install templates

```bash
dotnet new install ./templates
```

### 2. Generate an importer

```bash
dotnet new reckonry-importer -n Contoso.Reckonry.Importers.Example
```

The generated project contains:

- `src/Contoso.Reckonry.Importers.Example`
- `tests/Contoso.Reckonry.Importers.Example.Tests`
- `samples/fake-transactions.csv`
- `README.md`
- `ARCHITECTURE.md`

### 3. Run tests

If generated inside the Reckonry repository:

```bash
dotnet test Contoso.Reckonry.Importers.Example/tests/Contoso.Reckonry.Importers.Example.Tests/Contoso.Reckonry.Importers.Example.Tests.csproj
```

If generated elsewhere:

```bash
dotnet test Contoso.Reckonry.Importers.Example/tests/Contoso.Reckonry.Importers.Example.Tests/Contoso.Reckonry.Importers.Example.Tests.csproj -p:ReckonryRoot=/path/to/Reckonry
```

### 4. Inspect the importer contract

The generated importer implements:

```csharp
public interface ISourceImporter
{
    ImporterDescriptor Descriptor { get; }

    IReadOnlyList<LedgerEvent> ImportFolder(string inputFolder);
}
```

The template parser reads fake CSV rows and returns canonical `LedgerEvent`
values.

### 5. Preserve unknown rows

Unsupported rows must become `LedgerEventType.Unknown` events and must preserve
the raw source row:

```csharp
new SourceReference("Sample Provider", "fake-transactions.csv", rowNumber, rawRow)
```

Do not silently discard unsupported input. Do not invent missing amounts,
prices, fees, timestamps, classifications, or tax meanings.

### 6. Replace fake parsing carefully

When adapting the template to a real provider:

- add one test fixture per source schema
- add tests for unsupported rows
- keep raw row preservation
- keep tax interpretation out of the importer
- document supported files and operations in `ImporterDescriptor`
- keep all test data fake or anonymized

## Template-Specific Notes

Importer template:

- depends on `Reckonry.Core`
- depends on `Reckonry.Importers.Abstractions`
- implements `ISourceImporter`

Tax module template:

- depends on `Reckonry.Core`
- depends on `Reckonry.Tax.Abstractions`
- implements `ITaxModule`
- returns warnings only; it does not calculate tax

Report template:

- depends on `Reckonry.Core`
- depends on `Reckonry.Reports`
- implements `IReportModule`
- includes a module-specific markdown writer

Reconciliation template:

- depends on `Reckonry.Reconciliation.Abstractions`
- implements `IReconciliationModule`
- writes JSON and markdown summaries

## Current Limits

- External NuGet packages are not published yet.
- External binary plugin loading is not implemented yet.
- Templates use source references to a local Reckonry checkout.
- SDK contracts are pre-1.0 and may change with migration notes.

## Safety Rules

- Never commit real financial data.
- Never commit real exchange exports.
- Never commit generated private reports.
- Never invent financial values.
- Never make legal or tax certainty claims from a template.

