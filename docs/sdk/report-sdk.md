# Report SDK

Status: Draft architecture

The Report SDK lets third parties build reports from the canonical ledger.

## Responsibility

Reports transform read-only ledger data into files or structured outputs for review.

Reports must:

- Consume the canonical ledger as the source of truth.
- Never mutate ledger events.
- Be reproducible from the same ledger and options.
- Explain every generated number.
- Surface warnings for unknown or insufficient data.
- Avoid tax interpretation unless implemented inside a tax module.

## Core Interfaces

Proposed contracts:

```csharp
public interface ILedgerReport
{
    ReportDescriptor Descriptor { get; }

    Task<ReportResult> WriteAsync(
        ReportRequest request,
        IReadOnlyCollection<LedgerEvent> ledger,
        CancellationToken cancellationToken = default);
}
```

## Descriptor Metadata

Report descriptors should expose:

- `Id`
- `DisplayName`
- `ReportVersion`
- `SdkVersion`
- `SupportedLedgerSchemas`
- `OutputFormats`
- `RequiredInputs`
- `Stability`

## Dependency Injection

Report packages should register themselves as `ILedgerReport`:

```csharp
services.AddLedgerForgeRwReports();
```

Hosts should discover reports through:

```csharp
IEnumerable<ILedgerReport>
```

## Registration Rules

- Report ids must be stable and unique.
- Reports must write only to caller-provided output locations.
- Reports must not read private source files unless explicitly requested.
- Reports should include warnings for unknown events and missing values.
- Reports should include metadata needed to reproduce output.

## Versioning

Report plugins should declare:

- Plugin version.
- Compatible Report SDK version range.
- Supported canonical ledger schema versions.
- Output format version.

Breaking changes include:

- Removing columns.
- Renaming output fields.
- Changing calculation semantics.
- Changing required command options.

Breaking changes require migration notes.

## Future NuGet Package

Expected abstraction package:

```text
LedgerForge.Reports.Abstractions
```

Concrete report packages should depend on the abstraction package and `LedgerForge.Core`.
