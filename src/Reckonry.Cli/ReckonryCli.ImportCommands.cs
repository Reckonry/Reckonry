using Reckonry.Core;
using Reckonry.Importers.Abstractions;
using Reckonry.Tax.Italy.Rw;
using System.Reflection;

internal static partial class ReckonryCli
{
    private static async Task<int> ImportSourceAsync(string source, string[] args, AppServices services)
    {
        var input = GetOption(args, "--input");
        var output = GetOption(args, "--out");

        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(output))
        {
            WriteError("Missing required options: --input and --out.", "reckonry import <source> --input <folder> --out <ledger.json>");
            return ExitUsage;
        }

        if (!services.ImporterFactory.TryCreate(source, out var importer))
        {
            WriteError(
                $"No source importer is installed for `{source}`.",
                hint: "Run `reckonry plugins` to list installed modules.");
            return ExitUnavailable;
        }

        WriteInputSafetyWarning(input);

        IReadOnlyList<LedgerEvent> events;
        try
        {
            events = importer.ImportFolder(input);
        }
        catch (NotSupportedException ex)
        {
            WriteError(ex.Message);
            return ExitDataError;
        }

        await services.LedgerReportWriter.WriteAsync(output, events);

        Console.WriteLine($"Imported {events.Count} event(s) using {importer.Descriptor.DisplayName}.");
        Console.WriteLine($"Wrote ledger report to {output}.");
        Console.WriteLine($"Unknown events: {events.Count(e => e.EventType == LedgerEventType.Unknown)}.");
        return ExitSuccess;
    }

    private static async Task<int> ValidateAsync(string[] args, AppServices services)
    {
        var input = GetOption(args, "--input");

        if (string.IsNullOrWhiteSpace(input))
        {
            WriteError("Missing required option: --input.", "reckonry validate --input <ledger.json>");
            return ExitUsage;
        }

        WriteInputSafetyWarning(input);

        if (!File.Exists(input))
        {
            WriteError($"Ledger file was not found: {input}");
            return ExitNoInput;
        }

        var validation = await services.LedgerValidator.ValidateFileAsync(input);
        if (!validation.IsValid)
        {
            Console.Error.WriteLine($"Validation failed: {validation.Errors.Count} error(s).");
            foreach (var error in validation.Errors)
            {
                Console.Error.WriteLine($"  - {error}");
            }

            return ExitDataError;
        }

        Console.WriteLine($"Validation passed: {input}");
        return ExitSuccess;
    }
}
