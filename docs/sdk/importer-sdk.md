# Importer SDK

Status: Draft architecture

The Importer SDK lets third parties build exchange or wallet importers that produce canonical LedgerForge ledger events.

## Responsibility

Importers convert external source data into canonical ledger events.

Importers must:

- Preserve every source row or payload through `SourceReference`.
- Represent unsupported rows as `LedgerEventType.Unknown`.
- Use `decimal` for quantities and values.
- Normalize canonical event timestamps to UTC.
- Keep tax interpretation out of parsing.
- Never invent missing amounts, prices, fees, timestamps, or classifications.

## Core Interfaces

Proposed contracts:

```csharp
public interface IExchangeImporter
{
    ImporterDescriptor Descriptor { get; }

    IReadOnlyList<LedgerEvent> ImportFolder(string inputFolder);
}

public interface IImporterFactory
{
    IReadOnlyList<ImporterDescriptor> ListImporters();

    bool TryCreate(string importerIdOrExchange, out IExchangeImporter importer);

    IExchangeImporter CreateRequired(string importerIdOrExchange);
}
```

## Descriptor Metadata

Importer descriptors should expose:

- `Id`
- `DisplayName`
- `Provider`
- `ImporterVersion`
- `SdkVersion`
- `CoveragePercent`
- `SupportedFileExtensions`
- `SupportedFiles`
- `SupportedSchemas`
- `SupportedOperations`
- `SupportedLedgerSchemas`
- `Stability`

Coverage is informational. It must not imply correctness or tax validity.

## Dependency Injection

Importer packages should register themselves as `IExchangeImporter`:

```csharp
services.AddLedgerForgeBinanceImporter();
```

Hosts should consume all registered importers:

```csharp
var registry = new ImporterRegistry(importers);
var factory = new ImporterFactory(registry);
```

## Registration Rules

- Importer ids must be stable and unique.
- Duplicate ids should fail fast during host startup.
- Importers should be side-effect free until invoked.
- Real source data paths must be supplied by the caller.
- Importers must not write private data outside caller-selected output locations.

## Versioning

Importer plugins should declare:

- Plugin version.
- Compatible Importer SDK version range.
- Supported canonical ledger schema versions.
- Supported exchange export schemas.

Breaking importer behavior changes include:

- Reclassifying event types.
- Changing posting direction.
- Changing amount interpretation.
- Changing account mapping.
- Dropping source reference data.

Such changes require changelog entries and migration notes.

## Future NuGet Package

Expected abstraction package:

```text
LedgerForge.Importers.Abstractions
```

Concrete importer packages should depend on the abstraction package and `LedgerForge.Core`.
