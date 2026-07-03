using System.Diagnostics;
using System.Globalization;
using System.Text;
using Reckonry.Audit;
using Reckonry.Core;
using Reckonry.Importers.Binance;
using Reckonry.Reports;
using Reckonry.Storage;

var options = BenchmarkOptions.Parse(args);
var runner = new ReckonryBenchmarkRunner(options);
var reportPath = await runner.RunAsync();

Console.WriteLine($"Benchmark report written to {reportPath}");

internal sealed record BenchmarkOptions(
    string OutputPath,
    IReadOnlyList<int> Counts)
{
    public static BenchmarkOptions Parse(IReadOnlyList<string> args)
    {
        var outputPath = GetOption(args, "--out")
            ?? Path.Combine("benchmarks", "results", $"reckonry-benchmark-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.md");
        var counts = GetOption(args, "--counts") is { } countsText
            ? countsText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => int.Parse(value, CultureInfo.InvariantCulture))
                .ToArray()
            : [100, 1_000, 10_000, 100_000, 1_000_000];

        return new BenchmarkOptions(outputPath, counts);
    }

    private static string? GetOption(IReadOnlyList<string> args, string name)
    {
        for (var i = 0; i < args.Count - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }
}

internal sealed class ReckonryBenchmarkRunner
{
    private readonly BenchmarkOptions options;
    private readonly BinanceCsvImporter importer = new();
    private readonly JsonLedgerStore ledgerStore = new();
    private readonly RwSnapshotReportWriter rwSnapshotReportWriter = new();
    private readonly IntegrityChecker integrityChecker = new();

    public ReckonryBenchmarkRunner(BenchmarkOptions options)
    {
        this.options = options;
    }

    public async Task<string> RunAsync(CancellationToken cancellationToken = default)
    {
        var outputPath = Path.GetFullPath(options.OutputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");

        var results = new List<ScenarioResult>();

        foreach (var transactionCount in options.Counts)
        {
            results.Add(await RunScenarioAsync(transactionCount, cancellationToken));
        }

        await File.WriteAllTextAsync(outputPath, BuildMarkdown(results), cancellationToken);
        return outputPath;
    }

    private async Task<ScenarioResult> RunScenarioAsync(int transactionCount, CancellationToken cancellationToken)
    {
        var scenarioFolder = Path.Combine(Path.GetTempPath(), "reckonry-benchmarks", Guid.NewGuid().ToString("N"));
        var inputFolder = Path.Combine(scenarioFolder, "input");
        var outputFolder = Path.Combine(scenarioFolder, "output");
        Directory.CreateDirectory(inputFolder);
        Directory.CreateDirectory(outputFolder);

        try
        {
            var csvPath = Path.Combine(inputFolder, "binance-transactions.csv");
            var inputGeneration = await MeasureAsync(
                "Input generation",
                async () => await WriteFakeBinanceCsvAsync(csvPath, transactionCount, cancellationToken));

            IReadOnlyList<LedgerEvent> events = [];
            var parsing = Measure(
                "Parsing",
                () => events = importer.ImportFolder(inputFolder));

            var ledgerPath = Path.Combine(outputFolder, "ledger.json");
            var ledgerGeneration = await MeasureAsync(
                "Ledger generation",
                async () => await ledgerStore.WriteAsync(ledgerPath, events, cancellationToken));

            var rwGeneration = await MeasureAsync(
                "RW generation",
                async () => await rwSnapshotReportWriter.WriteAsync(outputFolder, 2025, events, cancellationToken));

            LedgerIntegrityReport? integrityReport = null;
            var audit = Measure(
                "Audit",
                () => integrityReport = integrityChecker.Check(events));

            return new ScenarioResult(
                transactionCount,
                events.Count,
                inputGeneration,
                parsing,
                ledgerGeneration,
                rwGeneration,
                audit,
                integrityReport?.IntegrityScore ?? 0,
                integrityReport?.ConfidenceScore ?? 0,
                FileSizeBytes(csvPath),
                FileSizeBytes(ledgerPath));
        }
        finally
        {
            if (Directory.Exists(scenarioFolder))
            {
                Directory.Delete(scenarioFolder, recursive: true);
            }
        }
    }

    private static async Task WriteFakeBinanceCsvAsync(
        string csvPath,
        int transactionCount,
        CancellationToken cancellationToken)
    {
        await using var stream = File.Create(csvPath);
        await using var writer = new StreamWriter(stream, Encoding.UTF8);
        await writer.WriteLineAsync("UTC_Time,Account,Operation,Coin,Change,Remark".AsMemory(), cancellationToken);

        var start = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var assets = new[] { "BTC", "ETH", "USDT", "BNB", "ADA" };

        for (var i = 0; i < transactionCount; i++)
        {
            var timestamp = start.AddSeconds(i).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            var asset = assets[i % assets.Length];
            var amount = ((i % 1000) + 1) / 1000m;
            await writer.WriteLineAsync(
                $"{timestamp},Spot,Deposit,{asset},{amount.ToString("0.00000000", CultureInfo.InvariantCulture)},Synthetic benchmark row".AsMemory(),
                cancellationToken);
        }
    }

    private static OperationResult Measure(string operationName, Action action)
    {
        Collect();
        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        var memoryBefore = GC.GetTotalMemory(forceFullCollection: false);
        var stopwatch = Stopwatch.StartNew();

        action();

        stopwatch.Stop();
        var memoryAfter = GC.GetTotalMemory(forceFullCollection: false);
        var allocatedAfter = GC.GetTotalAllocatedBytes(precise: true);

        return new OperationResult(
            operationName,
            stopwatch.Elapsed,
            Math.Max(memoryBefore, memoryAfter),
            Math.Max(0, allocatedAfter - allocatedBefore));
    }

    private static async Task<OperationResult> MeasureAsync(string operationName, Func<Task> action)
    {
        Collect();
        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        var memoryBefore = GC.GetTotalMemory(forceFullCollection: false);
        var stopwatch = Stopwatch.StartNew();

        await action();

        stopwatch.Stop();
        var memoryAfter = GC.GetTotalMemory(forceFullCollection: false);
        var allocatedAfter = GC.GetTotalAllocatedBytes(precise: true);

        return new OperationResult(
            operationName,
            stopwatch.Elapsed,
            Math.Max(memoryBefore, memoryAfter),
            Math.Max(0, allocatedAfter - allocatedBefore));
    }

    private static void Collect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private static long FileSizeBytes(string path)
    {
        return File.Exists(path) ? new FileInfo(path).Length : 0;
    }

    private static string BuildMarkdown(IReadOnlyList<ScenarioResult> results)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Reckonry Benchmark Report");
        builder.AppendLine();
        builder.AppendLine($"Generated at UTC: `{DateTimeOffset.UtcNow:O}`");
        builder.AppendLine();
        builder.AppendLine("Synthetic benchmark data only. No real financial data is used.");
        builder.AppendLine();
        builder.AppendLine("## Scope");
        builder.AppendLine();
        builder.AppendLine("- Parsing: Binance CSV importer over generated fake CSV rows.");
        builder.AppendLine("- Ledger generation: canonical `ledger.json` write and validation.");
        builder.AppendLine("- RW generation: yearly RW snapshot report generation.");
        builder.AppendLine("- Audit: in-memory ledger integrity check.");
        builder.AppendLine("- Memory: managed heap observed after each operation and process-wide allocated bytes during each operation.");
        builder.AppendLine();
        builder.AppendLine("## Results");
        builder.AppendLine();
        builder.AppendLine("| Transactions | Events | Parsing ms | Ledger ms | RW ms | Audit ms | Max observed heap MB | Allocated MB | CSV MB | Ledger JSON MB | Integrity | Confidence |");
        builder.AppendLine("| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");

        foreach (var result in results)
        {
            var operations = result.MeasuredOperations;
            builder
                .Append("| ")
                .Append(result.TransactionCount.ToString("N0", CultureInfo.InvariantCulture))
                .Append(" | ")
                .Append(result.EventCount.ToString("N0", CultureInfo.InvariantCulture))
                .Append(" | ")
                .Append(FormatMilliseconds(result.Parsing.Elapsed))
                .Append(" | ")
                .Append(FormatMilliseconds(result.LedgerGeneration.Elapsed))
                .Append(" | ")
                .Append(FormatMilliseconds(result.RwGeneration.Elapsed))
                .Append(" | ")
                .Append(FormatMilliseconds(result.Audit.Elapsed))
                .Append(" | ")
                .Append(FormatMegabytes(operations.Max(o => o.ObservedManagedHeapBytes)))
                .Append(" | ")
                .Append(FormatMegabytes(operations.Sum(o => o.AllocatedBytes)))
                .Append(" | ")
                .Append(FormatMegabytes(result.CsvSizeBytes))
                .Append(" | ")
                .Append(FormatMegabytes(result.LedgerJsonSizeBytes))
                .Append(" | ")
                .Append(result.IntegrityScore.ToString(CultureInfo.InvariantCulture))
                .Append(" | ")
                .Append(result.ConfidenceScore.ToString(CultureInfo.InvariantCulture))
                .AppendLine(" |");
        }

        builder.AppendLine();
        builder.AppendLine("## Detailed Operations");
        builder.AppendLine();

        foreach (var result in results)
        {
            builder.AppendLine($"### {result.TransactionCount.ToString("N0", CultureInfo.InvariantCulture)} Transactions");
            builder.AppendLine();
            builder.AppendLine("| Operation | Elapsed ms | Observed heap MB | Allocated MB |");
            builder.AppendLine("| --- | ---: | ---: | ---: |");
            foreach (var operation in result.MeasuredOperations)
            {
                builder
                    .Append("| ")
                    .Append(operation.Name)
                    .Append(" | ")
                    .Append(FormatMilliseconds(operation.Elapsed))
                    .Append(" | ")
                    .Append(FormatMegabytes(operation.ObservedManagedHeapBytes))
                    .Append(" | ")
                    .Append(FormatMegabytes(operation.AllocatedBytes))
                    .AppendLine(" |");
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string FormatMilliseconds(TimeSpan elapsed)
    {
        return elapsed.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string FormatMegabytes(long bytes)
    {
        return (bytes / 1024m / 1024m).ToString("0.###", CultureInfo.InvariantCulture);
    }
}

internal sealed record OperationResult(
    string Name,
    TimeSpan Elapsed,
    long ObservedManagedHeapBytes,
    long AllocatedBytes);

internal sealed record ScenarioResult(
    int TransactionCount,
    int EventCount,
    OperationResult InputGeneration,
    OperationResult Parsing,
    OperationResult LedgerGeneration,
    OperationResult RwGeneration,
    OperationResult Audit,
    int IntegrityScore,
    int ConfidenceScore,
    long CsvSizeBytes,
    long LedgerJsonSizeBytes)
{
    public IReadOnlyList<OperationResult> MeasuredOperations =>
    [
        InputGeneration,
        Parsing,
        LedgerGeneration,
        RwGeneration,
        Audit
    ];
}
