using System.Text.Json;
using System.Text.Json.Serialization;
using LedgerForge.Core;
using LedgerForge.Importers.Binance;
using LedgerForge.Reconciliation;
using LedgerForge.Reports;

return await LedgerForgeCli.RunAsync(args);

internal static class LedgerForgeCli
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0 || IsHelp(args))
        {
            PrintHelp();
            return 0;
        }

        if (args is ["import", "binance", .. var importArgs])
        {
            return await ImportBinanceAsync(importArgs);
        }

        if (args is ["validate", .. var validateArgs])
        {
            return await ValidateAsync(validateArgs);
        }

        if (args is ["report", "rw-snapshot", .. var reportArgs])
        {
            return await ReportRwSnapshotAsync(reportArgs);
        }

        if (args is ["report", "rw-value", .. var valueReportArgs])
        {
            return await ReportRwValueAsync(valueReportArgs);
        }

        if (args is ["reconcile", "binance", .. var reconcileArgs])
        {
            return await ReconcileBinanceAsync(reconcileArgs);
        }

        Console.Error.WriteLine("Unknown command. Run `ledgerforge --help` for usage.");
        return 1;
    }

    private static async Task<int> ImportBinanceAsync(string[] args)
    {
        var input = GetOption(args, "--input");
        var output = GetOption(args, "--out");

        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(output))
        {
            Console.Error.WriteLine("Missing required options. Usage: ledgerforge import binance --input <folder> --out <ledger.json>");
            return 1;
        }

        WriteInputSafetyWarning(input);

        var importer = new BinanceCsvImporter();
        var writer = new LedgerReportWriter();

        var events = importer.ImportFolder(input);
        await writer.WriteAsync(output, events);

        Console.WriteLine($"Imported {events.Count} event(s) from Binance CSV files.");
        Console.WriteLine($"Wrote ledger report to {output}.");
        Console.WriteLine($"Unknown events: {events.Count(e => e.EventType == LedgerEventType.Unknown)}.");
        return 0;
    }

    private static async Task<int> ReconcileBinanceAsync(string[] args)
    {
        var officialReports = GetOption(args, "--reports");
        var ledgerForgeReports = GetOption(args, "--ledger-reports");
        var outputFolder = GetOption(args, "--out");

        if (string.IsNullOrWhiteSpace(officialReports)
            || string.IsNullOrWhiteSpace(ledgerForgeReports)
            || string.IsNullOrWhiteSpace(outputFolder))
        {
            Console.Error.WriteLine("Missing required options. Usage: ledgerforge reconcile binance --reports <official-pdfs> --ledger-reports <reports> --out <output>");
            return 1;
        }

        WriteInputSafetyWarning(officialReports);

        var engine = new BinanceReconciliationEngine();
        var summary = await engine.ReconcileAsync(officialReports, ledgerForgeReports, outputFolder);

        Console.WriteLine($"Wrote Binance reconciliation summary to {outputFolder}.");
        foreach (var document in summary.Documents)
        {
            Console.WriteLine(
                $"ReportType={document.ReportType}; Year={document.Year?.ToString() ?? "Unknown"}; ExtractionSucceeded={document.ExtractionSucceeded}; Fields={document.ExtractedFieldCount}; Status={document.Status}");
        }

        return 0;
    }

    private static async Task<int> ReportRwValueAsync(string[] args)
    {
        var input = GetOption(args, "--input");
        var yearText = GetOption(args, "--year");
        var outputFolder = GetOption(args, "--out");

        if (string.IsNullOrWhiteSpace(input)
            || string.IsNullOrWhiteSpace(yearText)
            || string.IsNullOrWhiteSpace(outputFolder))
        {
            Console.Error.WriteLine("Missing required options. Usage: ledgerforge report rw-value --input <ledger.json> --year <year> --out <reports>");
            return 1;
        }

        WriteInputSafetyWarning(input);

        if (!int.TryParse(yearText, out var year) || year is < 1 or > 9999)
        {
            Console.Error.WriteLine($"Invalid year: {yearText}");
            return 1;
        }

        if (!File.Exists(input))
        {
            Console.Error.WriteLine($"Ledger file was not found: {input}");
            return 1;
        }

        var events = await ReadLedgerEventsAsync(input);
        var writer = new RwValueReportWriter();
        var rows = await writer.WriteAsync(outputFolder, year, events);

        Console.WriteLine($"Wrote RW value report for {year} to {outputFolder}.");
        Console.WriteLine($"Assets included: {rows.Count}.");
        Console.WriteLine($"Warnings: {rows.Count(r => !string.IsNullOrWhiteSpace(r.Warning))}.");
        return 0;
    }

    private static async Task<int> ValidateAsync(string[] args)
    {
        var input = GetOption(args, "--input");

        if (string.IsNullOrWhiteSpace(input))
        {
            Console.Error.WriteLine("Missing required option. Usage: ledgerforge validate --input <ledger.json>");
            return 1;
        }

        WriteInputSafetyWarning(input);

        if (!File.Exists(input))
        {
            Console.Error.WriteLine($"Ledger file was not found: {input}");
            return 1;
        }

        var events = await ReadLedgerEventsAsync(input);

        Console.WriteLine($"Ledger file is readable. Events: {events.Count}.");
        return 0;
    }

    private static async Task<int> ReportRwSnapshotAsync(string[] args)
    {
        var input = GetOption(args, "--input");
        var yearText = GetOption(args, "--year");
        var outputFolder = GetOption(args, "--out");

        if (string.IsNullOrWhiteSpace(input)
            || string.IsNullOrWhiteSpace(yearText)
            || string.IsNullOrWhiteSpace(outputFolder))
        {
            Console.Error.WriteLine("Missing required options. Usage: ledgerforge report rw-snapshot --input <ledger.json> --year <year> --out <reports>");
            return 1;
        }

        WriteInputSafetyWarning(input);

        if (!int.TryParse(yearText, out var year) || year is < 1 or > 9999)
        {
            Console.Error.WriteLine($"Invalid year: {yearText}");
            return 1;
        }

        if (!File.Exists(input))
        {
            Console.Error.WriteLine($"Ledger file was not found: {input}");
            return 1;
        }

        var events = await ReadLedgerEventsAsync(input);
        var writer = new RwSnapshotReportWriter();
        var rows = await writer.WriteAsync(outputFolder, year, events);

        Console.WriteLine($"Wrote RW snapshot report for {year} to {outputFolder}.");
        Console.WriteLine($"Assets included: {rows.Count}.");
        Console.WriteLine($"Unknown event warnings: {rows.Count(r => r.UnknownEventCount > 0)}.");
        return 0;
    }

    private static async Task<IReadOnlyList<LedgerEvent>> ReadLedgerEventsAsync(string input)
    {
        await using var stream = File.OpenRead(input);
        return await JsonSerializer.DeserializeAsync<IReadOnlyList<LedgerEvent>>(stream, JsonOptions)
            ?? Array.Empty<LedgerEvent>();
    }

    private static void WriteInputSafetyWarning(string input)
    {
        var warning = RepositoryInputSafety.BuildTrackedFolderWarning(input, Directory.GetCurrentDirectory());
        if (!string.IsNullOrWhiteSpace(warning))
        {
            Console.Error.WriteLine(warning);
        }
    }

    private static bool IsHelp(IReadOnlyList<string> args)
    {
        return args.Count == 1 && (args[0] is "--help" or "-h" or "help");
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

    private static void PrintHelp()
    {
        Console.WriteLine(
            """
            LedgerForge - crypto ledger engine

            Usage:
              ledgerforge --help
              ledgerforge import binance --input <folder> --out <ledger.json>
              ledgerforge validate --input <ledger.json>
              ledgerforge report rw-snapshot --input <ledger.json> --year <year> --out <reports>
              ledgerforge report rw-value --input <ledger.json> --year <year> --out <reports>
              ledgerforge reconcile binance --reports <official-pdfs> --ledger-reports <reports> --out <output>
            """);
    }
}
