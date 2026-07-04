using Reckonry.Core;
using Reckonry.Importers.Abstractions;
using Reckonry.Tax.Italy.Rw;
using System.Reflection;

internal static partial class ReckonryCli
{
    private static bool IsHelp(IReadOnlyList<string> args)
    {
        return args.Count == 1 && (args[0] is "--help" or "-h" or "help");
    }

    private static bool HasHelpFlag(IReadOnlyList<string> args)
    {
        return args.Any(arg => arg is "--help" or "-h");
    }

    private static bool IsVersion(IReadOnlyList<string> args)
    {
        return args.Count == 1 && (args[0] is "--version" or "-v" or "version");
    }

    private static void PrintHelp()
    {
        WriteBrand();
        WriteSection("Usage");
        Console.WriteLine("  reckonry <command> [options]");
        WriteSection("Global options");
        Console.WriteLine("  -h, --help       Show help.");
        Console.WriteLine("  -v, --version    Show version.");
        WriteSection("Health checks");
        Console.WriteLine("  reckonry doctor");
        Console.WriteLine("  reckonry doctor plugins");
        Console.WriteLine("  reckonry doctor privacy");
        Console.WriteLine("  reckonry doctor environment");
        Console.WriteLine("  reckonry doctor sdk");
        Console.WriteLine("  reckonry doctor demo");
        Console.WriteLine("  reckonry doctor repository");
        WriteSection("Core workflow");
        Console.WriteLine("  reckonry import <source> --input <folder> --out <ledger.json>");
        Console.WriteLine("  reckonry validate --input <ledger.json>");
        Console.WriteLine("  reckonry explain --input <artifact> [--ledger <ledger.json>] [--year <year>] [--out <explanation.md>]");
        Console.WriteLine("  reckonry plugins");
        WriteSection("Generic reports");
        Console.WriteLine("  reckonry report audit --input <ledger.json> --out <output>");
        Console.WriteLine("  reckonry report integrity --input <ledger.json> --out <output>");
        WriteSection("Reconciliation");
        Console.WriteLine("  reckonry reconcile <provider> <country> --reports <official-pdfs> --ledger-reports <reports> --out <output>");
        WriteSection("Country modules");
        Console.WriteLine("  reckonry tax italy rw template --year <year> --ledger <ledger.json> --out <config.json>");
        Console.WriteLine("  reckonry tax italy rw fill binance --config <config.json> --reconciliation <reconciliation-summary.json> --out <config.json>");
        Console.WriteLine("  reckonry tax italy rw snapshot --input <ledger.json> --year <year> --out <reports>");
        Console.WriteLine("  reckonry tax italy rw value --input <ledger.json> --year <year> --out <reports>");
        Console.WriteLine("  reckonry tax italy accountant --input <ledger.json> --year <year> --out <output> [--language it-IT|en-US]");
        Console.WriteLine("  reckonry tax italy dossier --year <year> --ledger <ledger.json> --handoff <accountant-handoff.json> --rw <italy-rw-accountant.json> --out <output> [--language it-IT|en-US]");
        WriteSection("Examples");
        Console.WriteLine("  reckonry doctor");
        Console.WriteLine("  reckonry plugins");
        Console.WriteLine("  reckonry import binance --input ./input/binance --out ./output/ledger.json");
        Console.WriteLine("  reckonry validate --input ./output/ledger.json");
        Console.WriteLine("  reckonry explain --input ./output/reports/rw-snapshot-2025.json --ledger ./output/ledger.json");
        Console.WriteLine("  reckonry report integrity --input ./output/ledger.json --out ./output/audit");
        Console.WriteLine("  reckonry reconcile coinbase global --reports ./input/coinbase-official --ledger-reports ./output/coinbase --out ./output/coinbase/reconciliation");
        WriteSection("Exit codes");
        Console.WriteLine(
            """
              0   Success.
              64  Command usage error.
              65  Input data validation error.
              66  Required input file was not found.
              69  Requested module is not installed or unavailable.
            """);
        WriteNext("reckonry doctor");
    }

    private static bool TryPrintCommandHelp(IReadOnlyList<string> args)
    {
        if (!HasHelpFlag(args))
        {
            return false;
        }

        if (args is ["import", ..])
        {
            PrintCommandHelp(
                "Import source data into a canonical ledger.",
                "reckonry import <source> --input <folder> --out <ledger.json>",
                ["reckonry import binance --input ./input/binance --out ./output/ledger.json"]);
            return true;
        }

        if (args is ["validate", ..])
        {
            PrintCommandHelp(
                "Validate a canonical ledger file.",
                "reckonry validate --input <ledger.json>",
                ["reckonry validate --input ./output/ledger.json"]);
            return true;
        }

        if (args is ["plugins", ..])
        {
            PrintCommandHelp(
                "List installed Reckonry modules.",
                "reckonry plugins",
                ["reckonry plugins"]);
            return true;
        }

        if (args is ["explain", ..])
        {
            PrintCommandHelp(
                "Explain how reported numbers trace back to source rows, ledger events, postings, and report rows.",
                "reckonry explain --input <artifact> [--ledger <ledger.json>] [--year <year>] [--out <explanation.md>]",
                [
                    "reckonry explain --input ./output/ledger.json",
                    "reckonry explain --input ./output/reports/rw-snapshot-2025.json --ledger ./output/ledger.json",
                    "reckonry explain ./output/audit/integrity.json --ledger ./output/ledger.json"
                ]);
            return true;
        }

        if (args is ["doctor", ..])
        {
            PrintCommandHelp(
                "Run local health checks for the repository, SDK, plugins, demo, privacy posture, and environment.",
                "reckonry doctor [plugins|privacy|environment|sdk|demo|repository]",
                [
                    "reckonry doctor",
                    "reckonry doctor sdk",
                    "reckonry doctor demo"
                ]);
            return true;
        }

        if (args is ["report", "audit", ..] or ["report", "integrity", ..] or ["audit", ..])
        {
            PrintCommandHelp(
                "Generate a generic ledger integrity report.",
                "reckonry report integrity --input <ledger.json> --out <output>",
                ["reckonry report integrity --input ./output/ledger.json --out ./output/audit"]);
            return true;
        }

        if (args is ["reconcile", ..])
        {
            PrintCommandHelp(
                "Compare generated report outputs with provider/country evidence.",
                "reckonry reconcile <provider> <country> --reports <official-pdfs> --ledger-reports <reports> --out <output>",
                [
                    "reckonry reconcile binance italy --reports ./input/binance --ledger-reports ./output/reports --out ./output/reconciliation",
                    "reckonry reconcile coinbase global --reports ./input/coinbase-official --ledger-reports ./output/coinbase --out ./output/coinbase/reconciliation"
                ]);
            return true;
        }

        if (args is ["tax", "italy", "rw", "template", ..])
        {
            PrintCommandHelp(
                "Generate a private Italy RW configuration template.",
                "reckonry tax italy rw template --year <year> --ledger <ledger.json> --out <config.json>",
                ["reckonry tax italy rw template --year 2025 --ledger ./output/ledger.json --out ./input/italy-rw/config.json"]);
            return true;
        }

        if (args is ["tax", "italy", "rw", "fill", "binance", ..])
        {
            PrintCommandHelp(
                "Fill an Italy RW configuration from Binance Italy reconciliation evidence when fields are unambiguous.",
                "reckonry tax italy rw fill binance --config <config.json> --reconciliation <reconciliation-summary.json> --out <config.json>",
                ["reckonry tax italy rw fill binance --config ./input/italy-rw/config.json --reconciliation ./output/reconciliation/reconciliation-summary.json --out ./input/italy-rw/config.filled.json"]);
            return true;
        }

        if (args is ["tax", "italy", "rw", "snapshot", ..])
        {
            PrintCommandHelp(
                "Generate Italy RW snapshot rows for professional review.",
                "reckonry tax italy rw snapshot --input <ledger.json> --year <year> --out <reports>",
                ["reckonry tax italy rw snapshot --input ./output/ledger.json --year 2025 --out ./output/reports"]);
            return true;
        }

        if (args is ["tax", "italy", "rw", "value", ..])
        {
            PrintCommandHelp(
                "Generate Italy RW value rows for professional review.",
                "reckonry tax italy rw value --input <ledger.json> --year <year> --out <reports>",
                ["reckonry tax italy rw value --input ./output/ledger.json --year 2025 --out ./output/reports"]);
            return true;
        }

        if (args is ["tax", "italy", "accountant", ..])
        {
            PrintCommandHelp(
                "Generate an Italy accountant review package.",
                "reckonry tax italy accountant --input <ledger.json> --year <year> --out <output> [--language it-IT|en-US]",
                ["reckonry tax italy accountant --input ./output/ledger.json --year 2025 --out ./output/accountant --language it-IT"]);
            return true;
        }

        if (args is ["tax", "italy", "dossier", ..])
        {
            PrintCommandHelp(
                "Generate an Italy Tax Dossier PDF for professional review.",
                "reckonry tax italy dossier --year <year> --ledger <ledger.json> --handoff <accountant-handoff.json> --rw <italy-rw-accountant.json> --out <output> [--language it-IT|en-US]",
                ["reckonry tax italy dossier --year 2025 --ledger ./output/ledger.json --handoff ./output/accountant/accountant-handoff-2025.json --rw ./output/accountant/italy-rw-accountant-2025.json --out ./output/accountant --language en-US"]);
            return true;
        }

        PrintHelp();
        return true;
    }

    private static void PrintCommandHelp(string description, string usage, IReadOnlyList<string> examples)
    {
        WriteBrand();
        WriteSection("Command");
        Console.WriteLine(description);
        WriteSection("Usage");
        Console.WriteLine($"  {usage}");
        WriteSection("Examples");
        foreach (var example in examples)
        {
            Console.WriteLine($"  {example}");
        }

        WriteNext(examples[0]);
    }

    private static void PrintVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        var version = string.IsNullOrWhiteSpace(informationalVersion)
            ? assembly.GetName().Version?.ToString() ?? "Unknown"
            : informationalVersion;

        WriteBrand();
        WriteSection("Version");
        WriteInfo("CLI", version);
        WriteInfo("Runtime", Environment.Version);
        WriteInfo("OS", Environment.OSVersion);
        WriteNext("reckonry doctor");
    }

    private static void WriteInvalidYear(string yearText)
    {
        WriteError(
            $"Invalid year: {yearText}",
            hint: "Year must be a four-digit numeric value between 0001 and 9999.");
    }

}
