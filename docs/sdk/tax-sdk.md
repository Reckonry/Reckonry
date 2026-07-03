# Tax SDK

Status: Draft architecture

The Tax SDK lets third parties build country-specific tax modules that interpret the canonical ledger.

## Responsibility

Tax modules consume canonical ledger data and produce tax-oriented outputs.

Tax modules must:

- Consume the ledger as read-only input.
- Never mutate existing ledger events.
- Keep jurisdiction-specific rules outside `Reckonry.Core`.
- Make every generated number explainable.
- Produce warnings when source data is insufficient.
- Avoid financial estimation by default.

## Core Interfaces

Proposed contracts:

```csharp
public interface ITaxModule
{
    TaxModuleDescriptor Descriptor { get; }

    Task<TaxReportResult> GenerateAsync(
        TaxReportRequest request,
        IReadOnlyCollection<LedgerEvent> ledger,
        CancellationToken cancellationToken = default);
}
```

## Descriptor Metadata

Tax module descriptors should expose:

- `Id`
- `DisplayName`
- `Jurisdiction`
- `TaxYearSupport`
- `ModuleVersion`
- `SdkVersion`
- `SupportedLedgerSchemas`
- `SupportedReports`
- `Stability`

## Dependency Injection

Tax packages should register themselves as `ITaxModule`:

```csharp
services.AddReckonryItalyTaxModule();
```

Hosts should discover tax modules through:

```csharp
IEnumerable<ITaxModule>
```

## Registration Rules

- Tax modules must identify jurisdiction and tax year support.
- Tax modules must document assumptions.
- Tax modules must fail clearly when required ledger data is missing.
- Tax modules must not rewrite, enrich, or delete canonical ledger events.
- Tax outputs must include enough provenance to explain calculations.

## Versioning

Tax modules should declare:

- Plugin version.
- Compatible Tax SDK version range.
- Supported canonical ledger schema versions.
- Supported jurisdiction and year range.

Breaking changes include:

- Changing report semantics.
- Changing classification rules.
- Changing required input fields.
- Changing output file format.

Breaking changes require changelog entries and migration notes.

## Future NuGet Package

Expected abstraction package:

```text
Reckonry.Tax.Abstractions
```

Concrete tax modules should depend on the abstraction package and `Reckonry.Core`.
