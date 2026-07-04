# SampleReconciliation

Reckonry reconciliation plugin template.

This template shows the minimum shape of a reconciliation module:

- Implements `IReconciliationModule`.
- Reads external evidence and Reckonry-generated outputs.
- Writes JSON and markdown summaries.
- Preserves uncertainty through status values.
- Does not mutate the ledger.

## Build

```bash
dotnet test tests/SampleReconciliation.Tests/SampleReconciliation.Tests.csproj
```

When generated outside the Reckonry repository:

```bash
dotnet test tests/SampleReconciliation.Tests/SampleReconciliation.Tests.csproj -p:ReckonryRoot=/path/to/Reckonry
```

The fake sample data is aggregate-only and contains no private financial data.

