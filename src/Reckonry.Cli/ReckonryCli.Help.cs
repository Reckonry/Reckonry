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
        Console.WriteLine(
            """
            Reckonry CLI
            Financial ledger infrastructure for reproducible review artifacts.

            Usage:
              reckonry <command> [options]

            Global options:
              -h, --help       Show help.
              -v, --version    Show version.

            Core commands:
              reckonry import <source> --input <folder> --out <ledger.json>
              reckonry validate --input <ledger.json>
              reckonry plugins

            Generic reports:
              reckonry report audit --input <ledger.json> --out <output>
              reckonry report integrity --input <ledger.json> --out <output>

            Reconciliation:
              reckonry reconcile <provider> <country> --reports <official-pdfs> --ledger-reports <reports> --out <output>

            Country modules:
              reckonry tax italy rw template --year <year> --ledger <ledger.json> --out <config.json>
              reckonry tax italy rw fill binance --config <config.json> --reconciliation <reconciliation-summary.json> --out <config.json>
              reckonry tax italy rw snapshot --input <ledger.json> --year <year> --out <reports>
              reckonry tax italy rw value --input <ledger.json> --year <year> --out <reports>
              reckonry tax italy accountant --input <ledger.json> --year <year> --out <output> [--language it-IT|en-US]
              reckonry tax italy dossier --year <year> --ledger <ledger.json> --handoff <accountant-handoff.json> --rw <italy-rw-accountant.json> --out <output> [--language it-IT|en-US]

            Examples:
              reckonry plugins
              reckonry import binance --input ./input/binance --out ./output/ledger.json
              reckonry validate --input ./output/ledger.json
              reckonry report integrity --input ./output/ledger.json --out ./output/audit

            Exit codes:
              0   Success.
              64  Command usage error.
              65  Input data validation error.
              66  Required input file was not found.
              69  Requested module is not installed or unavailable.
            """);
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
                ["reckonry reconcile binance italy --reports ./input/binance --ledger-reports ./output/reports --out ./output/reconciliation"]);
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
        Console.WriteLine("Reckonry CLI");
        Console.WriteLine(description);
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine($"  {usage}");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        foreach (var example in examples)
        {
            Console.WriteLine($"  {example}");
        }
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

        Console.WriteLine($"reckonry {version}");
    }

    private static void WriteInvalidYear(string yearText)
    {
        WriteError(
            $"Invalid year: {yearText}",
            hint: "Year must be a four-digit numeric value between 0001 and 9999.");
    }

    private static void WriteError(string message, string? usage = null, string? hint = null)
    {
        Console.Error.WriteLine($"Error: {message}");
        if (!string.IsNullOrWhiteSpace(usage))
        {
            Console.Error.WriteLine($"Usage: {usage}");
        }

        if (!string.IsNullOrWhiteSpace(hint))
        {
            Console.Error.WriteLine($"Hint: {hint}");
        }
    }
}
