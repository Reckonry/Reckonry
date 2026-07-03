# Reconciliation SDK

Status: Draft architecture

The Reconciliation SDK lets third parties compare Reckonry outputs against external evidence such as official exchange reports.

## Responsibility

Reconciliation providers validate Reckonry results against external documents or datasets.

Reconciliation must:

- Be read-only.
- Never replace the canonical ledger.
- Never mutate ledger events.
- Preserve extraction status and uncertainty.
- Avoid OCR unless explicitly configured by the host.
- Avoid printing private financial values in logs or chat-oriented outputs.

## Core Interfaces

Proposed contracts:

```csharp
public interface IReconciliationProvider
{
    ReconciliationDescriptor Descriptor { get; }

    Task<ReconciliationResult> ReconcileAsync(
        ReconciliationRequest request,
        IReadOnlyCollection<LedgerEvent> ledger,
        CancellationToken cancellationToken = default);
}
```

Document readers should be separate dependencies:

```csharp
public interface IExternalReportReader<TReport>
{
    Task<TReport> ReadAsync(string path, CancellationToken cancellationToken = default);
}
```

## Descriptor Metadata

Reconciliation descriptors should expose:

- `Id`
- `DisplayName`
- `Provider`
- `ReconciliationVersion`
- `SdkVersion`
- `SupportedExternalReportTypes`
- `SupportedLedgerSchemas`
- `ExtractionMethods`
- `Stability`

## Dependency Injection

Reconciliation packages should register providers and document readers:

```csharp
services.AddReckonryBinanceReconciliation();
```

Hosts should discover reconciliation providers through:

```csharp
IEnumerable<IReconciliationProvider>
```

## Registration Rules

- Reconciliation providers must not treat external reports as the source of truth.
- The ledger remains the source of truth.
- Extraction failures must be explicit.
- Image-only documents must be detected and reported if OCR is required.
- Reconciliation summaries should expose status and field counts without leaking private values.

## Versioning

Reconciliation plugins should declare:

- Plugin version.
- Compatible Reconciliation SDK version range.
- Supported canonical ledger schema versions.
- Supported external report versions where known.

Breaking changes include:

- Changing reconciliation status semantics.
- Changing extracted field models.
- Changing output summary format.
- Changing document reader contracts.

Breaking changes require migration notes.

## Future NuGet Package

Expected abstraction package:

```text
Reckonry.Reconciliation.Abstractions
```

Concrete reconciliation packages should depend on the abstraction package and `Reckonry.Core`.
