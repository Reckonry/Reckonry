# SampleReport

Reckonry report plugin template.

This template shows the current report SDK shape:

- Implements `IReportModule` for descriptor discovery.
- Provides a module-specific writer service.
- Consumes canonical ledger events as read-only input.
- Writes a deterministic markdown review artifact.
- Includes tests and fake ledger data.

## Build

```bash
dotnet test tests/SampleReport.Tests/SampleReport.Tests.csproj
```

When generated outside the Reckonry repository:

```bash
dotnet test tests/SampleReport.Tests/SampleReport.Tests.csproj -p:ReckonryRoot=/path/to/Reckonry
```

`IReportModule` advertises report metadata only today. A common report execution
interface is planned but not implemented.

