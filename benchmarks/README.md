# LedgerForge Benchmarks

This folder contains synthetic performance benchmarks for LedgerForge.

The benchmark project measures:

- Parsing.
- Canonical ledger generation.
- RW snapshot generation.
- Audit integrity checks.
- Managed memory observations.

Default transaction counts:

- 100
- 1,000
- 10,000
- 100,000
- 1,000,000

Run:

```bash
dotnet run -c Release --project benchmarks/LedgerForge.Benchmarks/LedgerForge.Benchmarks.csproj
```

Custom output path:

```bash
dotnet run -c Release --project benchmarks/LedgerForge.Benchmarks/LedgerForge.Benchmarks.csproj -- --out benchmarks/results/report.md
```

Custom counts:

```bash
dotnet run -c Release --project benchmarks/LedgerForge.Benchmarks/LedgerForge.Benchmarks.csproj -- --counts 100,1000,10000
```

The benchmark uses generated fake data only. It must not read real exchange exports or write private financial data into benchmark reports.
