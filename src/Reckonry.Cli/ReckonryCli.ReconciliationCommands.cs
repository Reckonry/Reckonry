using Reckonry.Core;
using Reckonry.Importers.Abstractions;
using Reckonry.Tax.Italy.Rw;
using System.Reflection;

internal static partial class ReckonryCli
{
    private static async Task<int> ReconcileProviderCountryAsync(
        string provider,
        string country,
        string[] args,
        AppServices services)
    {
        var officialReports = GetOption(args, "--reports");
        var reckonryReports = GetOption(args, "--ledger-reports");
        var outputFolder = GetOption(args, "--out");

        if (string.IsNullOrWhiteSpace(officialReports)
            || string.IsNullOrWhiteSpace(reckonryReports)
            || string.IsNullOrWhiteSpace(outputFolder))
        {
            WriteError(
                "Missing required options: --reports, --ledger-reports, and --out.",
                "reckonry reconcile <provider> <country> --reports <official-pdfs> --ledger-reports <reports> --out <output>");
            return ExitUsage;
        }

        var countryCode = NormalizeCountryCode(country);
        if (!services.TryGetReconciliationModule(provider, countryCode, out var module))
        {
            WriteError(
                $"No reconciliation module is installed for provider `{provider}` and country `{country}`.",
                hint: "Run `reckonry plugins` to list installed reconciliation modules.");
            return ExitUnavailable;
        }

        WriteInputSafetyWarning(officialReports);
        WritePhase($"Reconciling {provider}/{country}");

        var result = await module.ReconcileAsync(new(officialReports, reckonryReports, outputFolder));
        dynamic summary = result.Summary;

        WriteSuccess($"{module.Descriptor.DisplayName} summary generated.");
        WriteInfo("Output", outputFolder);
        foreach (var document in summary.Documents)
        {
            Console.WriteLine(
                $"  {document.ReportType}: year={document.Year?.ToString() ?? "Unknown"}; extracted={document.ExtractionSucceeded}; fields={document.ExtractedFieldCount}; status={document.Status}");
        }

        var next = string.Equals(provider, "binance", StringComparison.OrdinalIgnoreCase)
            && string.Equals(countryCode, "IT", StringComparison.OrdinalIgnoreCase)
            ? "reckonry tax italy rw fill binance --config <config.json> --reconciliation <reconciliation-summary.json> --out <config.filled.json>"
            : "reckonry doctor plugins";
        WriteNext(next);
        return ExitSuccess;
    }

    private static string NormalizeCountryCode(string country)
    {
        return country.Equals("italy", StringComparison.OrdinalIgnoreCase) ? "IT" : country.ToUpperInvariant();
    }
}
