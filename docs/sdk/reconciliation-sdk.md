# Reconciliation SDK

Status: Current internal contracts, pre-stable.

The reconciliation contracts are implemented in
`Reckonry.Reconciliation.Abstractions`. They are used by bundled Reckonry
modules today. They are not stable external NuGet SDKs yet.

## Responsibility

Reconciliation modules compare Reckonry outputs against external documents or
datasets.

Reconciliation must:

- Be read-only.
- Never replace the canonical ledger.
- Never mutate ledger events.
- Preserve extraction status and uncertainty.
- Avoid OCR unless explicitly configured by the host.
- Avoid printing private financial values in logs or chat-oriented outputs.

## Implemented Interfaces

This interface matches the source code exactly.

```csharp
public interface IReconciliationModule
{
    ReconciliationModuleDescriptor Descriptor { get; }

    Task<ReconciliationRunResult> ReconcileAsync(
        ReconciliationRunRequest request,
        CancellationToken cancellationToken = default);
}
```

## Implemented Records

These records and enum match the source code exactly.

```csharp
public sealed record ReconciliationModuleDescriptor(
    string Id,
    string DisplayName,
    ReconciliationScope Scope,
    string? ProviderId,
    string? CountryCode,
    bool ProfessionalReviewRequired,
    IReadOnlyList<string> SupportedInputFormats,
    IReadOnlyList<string> GeneratedArtifacts);
```

```csharp
public sealed record ReconciliationRunRequest(
    string OfficialReportsFolder,
    string ReckonryReportsFolder,
    string OutputFolder);
```

```csharp
public sealed record ReconciliationRunResult(
    string ModuleId,
    string OutputFolder,
    IReadOnlyList<string> GeneratedFileNames,
    object Summary);
```

```csharp
public enum ReconciliationScope
{
    Generic,
    Provider,
    Country,
    ProviderCountry
}
```

## Implemented Discovery Model

Hosts discover bundled reconciliation modules through `Reckonry.Plugins`:

```csharp
var plugins = PluginScanner.ScanPlugins();
var reconciliationModules = plugins.ReconciliationModules;
```

Discovery loads non-abstract, non-interface types assignable to
`IReconciliationModule` from Reckonry assemblies in the host output.
Constructors must have only optional parameters or no parameters.

This is bundled assembly discovery. It is not external binary plugin loading.

## Current Reconciliation Modules

| Id | DisplayName | Scope | ProviderId | CountryCode | ProfessionalReviewRequired | Inputs | Artifacts |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `binance-italy` | `Binance Italy Reconciliation` | `ProviderCountry` | `binance` | `IT` | `true` | `pdf` | `reconciliation-summary.json`, `reconciliation-summary.md` |
| `coinbase-global` | `Coinbase Global Reconciliation` | `Provider` | `coinbase` | `null` | `false` | `csv` | `reconciliation-summary.json`, `reconciliation-summary.md` |

`BinanceReconciliationEngine` also exposes an implementation-specific overload:

```csharp
public Task<BinanceReconciliationSummary> ReconcileAsync(
    string officialReportsFolder,
    string reckonryReportsFolder,
    string outputFolder,
    CancellationToken cancellationToken = default);
```

That overload is not part of `IReconciliationModule`.

`CoinbaseReconciliationEngine` implements only `IReconciliationModule`. It
compares synthetic aggregate statement metadata against generated Reckonry
ledger counts for the public demo and does not perform tax reconciliation,
balance verification, valuation, or official provider document extraction.

## Planned

The following reconciliation SDK metadata is planned but not implemented today:

- explicit SDK contract version
- supported ledger report schema versions
- typed `Summary` result instead of `object`
- compatibility range

## Versioning

Reconciliation contracts are pre-1.0 and may change with migration notes.

Breaking changes include:

- Changing reconciliation status semantics.
- Changing extracted field models.
- Changing output summary format.
- Changing document reader contracts.
