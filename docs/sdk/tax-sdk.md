# Tax SDK

Status: Current internal contracts, pre-stable.

The tax contracts are implemented in `Reckonry.Tax.Abstractions`. They are used
by bundled Reckonry modules today. They are not stable external NuGet SDKs yet.

## Responsibility

Tax modules consume canonical ledger data and produce country-specific outputs.

Tax modules must:

- Consume the ledger as read-only input.
- Never mutate existing ledger events.
- Keep jurisdiction-specific rules outside `Reckonry.Core`.
- Make every generated number explainable.
- Produce warnings when source data is insufficient.
- Avoid financial estimation by default.

## Implemented Interface

This interface matches the source code exactly.

```csharp
public interface ITaxModule
{
    TaxModuleDescriptor Descriptor { get; }

    TaxReportResult Analyze(TaxReportRequest request);
}
```

## Implemented Records

These records and enum match the source code exactly.

```csharp
public sealed record TaxModuleDescriptor(
    string CountryCode,
    string DisplayName,
    string Version)
{
    public string CountryName { get; init; } = DisplayName;

    public IReadOnlyList<int> SupportedTaxYears { get; init; } = [];

    public IReadOnlyList<TaxOfficialSource> OfficialSources { get; init; } = [];

    public IReadOnlyList<TaxRequiredInput> RequiredInputs { get; init; } = [];

    public IReadOnlyList<TaxGeneratedArtifact> GeneratedArtifacts { get; init; } = [];

    public IReadOnlyList<TaxConfigurationSchema> ConfigurationSchemas { get; init; } = [];

    public TaxCompatibility Compatibility { get; init; } = new(
        "reckonry-ledger-v1",
        "0.1.0-alpha",
        []);

    public ProfessionalReviewStatus ProfessionalReviewStatus { get; init; } =
        ProfessionalReviewStatus.Required;
}
```

```csharp
public sealed record TaxReportRequest(
    int Year,
    IReadOnlyCollection<LedgerEvent> LedgerEvents);
```

```csharp
public sealed record TaxReportResult(
    TaxModuleDescriptor Module,
    int Year,
    IReadOnlyList<string> Warnings);
```

```csharp
public sealed record TaxOfficialSource(
    string Title,
    string Publisher,
    string? Url,
    string? Version,
    DateOnly? PublishedDate);
```

```csharp
public sealed record TaxRequiredInput(
    string Id,
    string DisplayName,
    string Description,
    bool Required);
```

```csharp
public sealed record TaxGeneratedArtifact(
    string Id,
    string DisplayName,
    string OutputFormat,
    bool ProfessionalReviewRequired);
```

```csharp
public sealed record TaxConfigurationSchema(
    string Id,
    string DisplayName,
    string Format,
    string? SchemaPath);
```

```csharp
public sealed record TaxCompatibility(
    string LedgerSchema,
    string MinimumReckonryVersion,
    IReadOnlyList<string> Notes);
```

```csharp
public enum ProfessionalReviewStatus
{
    Required,
    Recommended,
    NotRequired,
    Unknown
}
```

## Implemented Discovery Model

Hosts discover bundled tax modules through `Reckonry.Plugins`:

```csharp
var plugins = PluginScanner.ScanPlugins();
var taxModules = plugins.TaxModules;
```

Discovery loads non-abstract, non-interface types assignable to `ITaxModule`
from Reckonry assemblies in the host output. Constructors must have only
optional parameters or no parameters.

This is bundled assembly discovery. It is not external binary plugin loading.

## Current Tax Module

| CountryCode | DisplayName | Version | CountryName | SupportedTaxYears | ProfessionalReviewStatus |
| --- | --- | --- | --- | --- | --- |
| `IT` | `Italy` | `0.1.0` | `Italy` | `2025`, `2026` | `Required` |

The Italy module reports:

- one official source entry for Agenzia delle Entrate instructions, with no URL
  or published date recorded in the descriptor today
- required inputs: `ledger`, `rw-config`
- optional input: `official-reports`
- generated artifacts: `italy-rw`, `italy-accountant-package`,
  `italy-tax-dossier`
- configuration schema: `italy-rw-config`
- compatibility: `reckonry-ledger-v1`, minimum Reckonry `0.1.0-alpha`

`ItalyTaxModule.Analyze` currently returns a warning stating that the Italy tax
module is a placeholder and does not calculate taxes, capital gains, LIFO, FIFO,
or legal advice.

Italy RW writers and the Tax Dossier generator live under
`Reckonry.Tax.Italy.Rw`. They are implementation-specific services, not members
of `ITaxModule`.

## Planned

The following tax SDK behavior is planned but not implemented in `ITaxModule`
today:

- common artifact generation through the tax module interface
- asynchronous tax module analysis
- machine-readable JSON schemas for every configuration descriptor
- stable external country-module package contracts

## Versioning

Tax contracts are pre-1.0 and may change with migration notes.

Breaking changes include:

- Changing report semantics.
- Changing classification rules.
- Changing required input fields.
- Changing output file format.
