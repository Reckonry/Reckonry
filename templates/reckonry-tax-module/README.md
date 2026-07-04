# SampleTaxModule

Reckonry tax module plugin template.

This template shows the minimum shape of a country module:

- Implements `ITaxModule`.
- Advertises country metadata, official sources, required inputs, generated
  artifacts, and compatibility.
- Consumes canonical ledger events as read-only input.
- Returns warnings when professional inputs are missing.
- Does not calculate taxes or invent financial data.

## Build

```bash
dotnet test tests/SampleTaxModule.Tests/SampleTaxModule.Tests.csproj
```

When generated outside the Reckonry repository:

```bash
dotnet test tests/SampleTaxModule.Tests/SampleTaxModule.Tests.csproj -p:ReckonryRoot=/path/to/Reckonry
```

## Generate From Template

```bash
dotnet new reckonry-tax-module -n Contoso.Reckonry.Tax.Example
dotnet test Contoso.Reckonry.Tax.Example/tests/Contoso.Reckonry.Tax.Example.Tests/Contoso.Reckonry.Tax.Example.Tests.csproj -p:ReckonryRoot=/path/to/Reckonry
```

Professional review and official sources are required for real tax modules.

