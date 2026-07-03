# Report SDK

Status: Current internal contracts, pre-stable.

The report contracts are implemented in `Reckonry.Reports`. They are used by
bundled Reckonry modules today. They are not stable external NuGet SDKs yet.

## Responsibility

Reports transform read-only ledger data into files or descriptors for review.

Reports must:

- Consume the canonical ledger as the source of truth.
- Never mutate ledger events.
- Be reproducible from the same ledger and options.
- Explain every generated number.
- Surface warnings for unknown or insufficient data.
- Avoid tax interpretation unless implemented inside a tax module.

## Implemented Interfaces

These interfaces match the source code exactly.

```csharp
public interface IReportModule
{
    ReportDescriptor Descriptor { get; }
}
```

```csharp
public interface ILedgerReportWriter
{
    Task WriteAsync(
        string ledgerJsonPath,
        IReadOnlyCollection<LedgerEvent> events,
        CancellationToken cancellationToken = default);
}
```

`IReportModule` advertises report metadata. It does not define a common report
execution method today. Report writers use writer-specific interfaces.

## Implemented Descriptor

This record and enum match the source code exactly.

```csharp
public sealed record ReportDescriptor(
    string Id,
    string DisplayName,
    ReportScope Scope,
    string? CountryCode,
    string? ProviderId,
    bool ProfessionalReviewRequired,
    IReadOnlyList<string> SupportedOutputFormats);
```

```csharp
public enum ReportScope
{
    Generic,
    Country,
    Provider,
    Professional
}
```

## Implemented Discovery Model

Hosts discover bundled report modules through `Reckonry.Plugins`:

```csharp
var plugins = PluginScanner.ScanPlugins();
var reports = plugins.Reports;
```

Discovery loads non-abstract, non-interface types assignable to `IReportModule`
from Reckonry assemblies in the host output. Constructors must have only
optional parameters or no parameters.

This is bundled assembly discovery. It is not external binary plugin loading.

## Current Report Modules

| Id | DisplayName | Scope | CountryCode | ProviderId | ProfessionalReviewRequired | Formats |
| --- | --- | --- | --- | --- | --- | --- |
| `audit` | `Ledger Audit` | `Generic` | `null` | `null` | `false` | `json`, `md` |
| `integrity` | `Ledger Integrity` | `Generic` | `null` | `null` | `false` | `json`, `md` |
| `ledger` | `Canonical Ledger` | `Generic` | `null` | `null` | `false` | `json`, `csv` |
| `summary` | `Ledger Summary` | `Generic` | `null` | `null` | `false` | `json`, `csv`, `md` |
| `italy-rw-snapshot` | `Italy RW Snapshot` | `Country` | `IT` | `null` | `true` | `csv`, `json` |
| `italy-rw-value` | `Italy RW Value` | `Country` | `IT` | `null` | `true` | `csv`, `json` |
| `italy-rw-accountant-package` | `Italy RW Accountant Package` | `Professional` | `IT` | `null` | `true` | `json`, `csv`, `md` |
| `italy-tax-dossier` | `Italy Tax Dossier` | `Professional` | `IT` | `null` | `true` | `pdf` |

The Italy report modules live in `Reckonry.Tax.Italy.Rw`, not in generic
`Reckonry.Reports`.

## Planned

A common report execution interface is planned but not implemented today.
Current report generation uses writer-specific interfaces and CLI command
handlers.

## Versioning

Report contracts are pre-1.0 and may change with migration notes.

Breaking changes include:

- Removing columns.
- Renaming output fields.
- Changing calculation semantics.
- Changing required command options.
