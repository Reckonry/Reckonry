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
        WritePhase("Generating ledger integrity report");

        if (!File.Exists(input))
        {
            WriteError($"Ledger file was not found: {input}");
            return ExitNoInput;
        }

        var events = await services.LedgerStore.ReadAsync(input);
        var report = await services.IntegrityChecker.WriteAsync(outputFolder, events);

        WriteSuccess("Ledger integrity report generated.");
        WriteInfo("Output", outputFolder);
        WriteInfo("Integrity score", report.IntegrityScore);
        WriteInfo("Confidence score", report.ConfidenceScore);
        WriteInfo("Warnings", report.Warnings.Count);
        WriteInfo("Recommendations", report.Recommendations.Count);
        WriteNext("reckonry plugins");
        return ExitSuccess;
    }
}
