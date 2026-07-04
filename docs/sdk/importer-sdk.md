# Importer SDK

Status: Current internal contracts, pre-stable.

The importer contracts are implemented in `Reckonry.Importers.Abstractions`.
They are used by bundled Reckonry modules today. They are not stable external
NuGet SDKs yet.

## Responsibility

Importers convert external source data into canonical `LedgerEvent` values.

Implemented importers must:

- Preserve source rows or payloads through `SourceReference`.
- Represent unsupported rows as `LedgerEventType.Unknown`.
- Use `decimal` for quantities and values.
- Normalize canonical event timestamps to UTC.
- Keep tax interpretation out of parsing.
- Never invent missing amounts, prices, fees, timestamps, or classifications.

## Implemented Interfaces

These interfaces match the source code exactly.

```csharp
public interface ISourceImporter
{
    ImporterDescriptor Descriptor { get; }

    IReadOnlyList<LedgerEvent> ImportFolder(string inputFolder);
}
```

```csharp
public interface IExchangeImporter : ISourceImporter
{
}
```

```csharp
public interface ILedgerImporter : ISourceImporter
{
}
```

```csharp
public interface IImporterFactory
{
    IReadOnlyList<ImporterDescriptor> ListImporters();

    bool TryCreate(string importerIdOrSource, out ISourceImporter importer);

    ISourceImporter CreateRequired(string importerIdOrSource);
}
```

`IExchangeImporter` and `ILedgerImporter` are marker specializations. They do
not add members beyond `ISourceImporter`.

## Implemented Descriptor

This record matches the source code exactly.

```csharp
public sealed record ImporterDescriptor
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public required string Provider { get; init; }

    public SourceKind SourceKind { get; init; } = SourceKind.Exchange;

    public string Exchange => Provider;

    public required string ImporterVersion { get; init; }

    public required decimal CoveragePercent { get; init; }

    public required IReadOnlySet<string> SupportedFileExtensions { get; init; }

    public required IReadOnlyList<string> SupportedFiles { get; init; }

    public required IReadOnlyList<string> SupportedSchemas { get; init; }

    public required IReadOnlyList<string> SupportedOperations { get; init; }
}
```

```csharp
public enum SourceKind
{
    Exchange,
    Broker,
    Bank,
    Wallet,
    Custodian,
    GovernmentReport,
    ManualCsv,
    AccountingSystem,
    Other
}
```

`Exchange` is a compatibility alias for `Provider`. It remains in the
implemented descriptor today.

Coverage is informational. It must not imply correctness, completeness, or tax
validity.

## Implemented Registry and Factory Behavior

`ImporterRegistry` accepts `IEnumerable<ISourceImporter>` and registers lookup
keys for:

- `Descriptor.Id`
- `Descriptor.Provider`
- `Descriptor.DisplayName`
- a normalized alphanumeric lowercase provider key

Duplicate lookup keys fail with `InvalidOperationException`.

`ImporterFactory` wraps `ImporterRegistry` and exposes:

- `ListImporters()`
- `TryCreate(string importerIdOrSource, out ISourceImporter importer)`
- `CreateRequired(string importerIdOrSource)`

`CreateRequired` throws `KeyNotFoundException` when no importer matches.

## Implemented Discovery Model

Hosts discover bundled importers through `Reckonry.Plugins`:

```csharp
var plugins = PluginScanner.ScanPlugins();
var registry = new ImporterRegistry(plugins.Importers);
var factory = new ImporterFactory(registry);
```

Discovery loads non-abstract, non-interface types assignable to
`ISourceImporter` from Reckonry assemblies in the host output. Constructors must
have only optional parameters or no parameters.

This is bundled assembly discovery. It is not external binary plugin loading.

## Current Importer Modules

Implemented parser:

| Id | Provider | SourceKind | Version | Status |
| --- | --- | --- | --- | --- |
| `binance` | `Binance` | `Exchange` | `0.1.0` | Early CSV parser |

Registered placeholders:

| Id | Provider | SourceKind | Version | Status |
| --- | --- | --- | --- | --- |
| `bitstamp` | `Bitstamp` | `Exchange` | `0.0.0-placeholder` | Planned parser |
| `coinbase` | `Coinbase` | `Exchange` | `0.0.0-placeholder` | Planned parser |
| `crypto.com` | `Crypto.com` | `Exchange` | `0.0.0-placeholder` | Planned parser |
| `kraken` | `Kraken` | `Exchange` | `0.0.0-placeholder` | Planned parser |
| `revolut` | `Revolut` | `Broker` | `0.0.0-placeholder` | Planned parser |

Placeholder importers are discoverable but throw `NotSupportedException` from
`ImportFolder`.

## Planned

The following metadata is planned but not implemented in `ImporterDescriptor`
today:

- `SdkVersion`
- `SupportedLedgerSchemas`
- `Stability`
- explicit compatibility range

Future external importer packages may depend on `Reckonry.Importers.Abstractions`
and `Reckonry.Core`, but no external NuGet package contract is stable yet.

## Versioning

Importer contracts are pre-1.0 and may change with migration notes.

Breaking importer behavior changes include:

- Reclassifying event types.
- Changing posting direction.
- Changing amount interpretation.
- Changing account mapping.
- Dropping source reference data.
