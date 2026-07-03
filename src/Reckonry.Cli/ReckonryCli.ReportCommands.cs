using Reckonry.Core;
using Reckonry.Importers.Abstractions;
using Reckonry.Tax.Italy.Rw;
using System.Reflection;

internal static partial class ReckonryCli
{
    private static async Task<int> AuditAsync(string[] args, AppServices services)
    {
        var input = GetOption(args, "--input");
        var outputFolder = GetOption(args, "--out");

        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(outputFolder))
        {
            WriteError("Missing required options: --input and --out.", "reckonry report audit --input <ledger.json> --out <output>");
            return ExitUsage;
        }

        WriteInputSafetyWarning(input);

        if (!File.Exists(input))
        {
            WriteError($"Ledger file was not found: {input}");
            return ExitNoInput;
        }

        var events = await services.LedgerStore.ReadAsync(input);
        var report = await services.IntegrityChecker.WriteAsync(outputFolder, events);

        Console.WriteLine($"Wrote ledger integrity report to {outputFolder}.");
        Console.WriteLine($"Integrity Score: {report.IntegrityScore}");
        Console.WriteLine($"Confidence Score: {report.ConfidenceScore}");
        Console.WriteLine($"Warnings: {report.Warnings.Count}");
        Console.WriteLine($"Recommendations: {report.Recommendations.Count}");
        return ExitSuccess;
    }
}
