using Reckonry.Core;
using Reckonry.Importers.Abstractions;
using Reckonry.Tax.Italy.Rw;
using System.Reflection;

return await ReckonryCli.RunAsync(args, AppServices.CreateDefault());

internal static partial class ReckonryCli
{
    private const int ExitSuccess = 0;
    private const int ExitUsage = 64;
    private const int ExitDataError = 65;
    private const int ExitNoInput = 66;
    private const int ExitUnavailable = 69;
    private const string SuppressRepositoryInputWarningVariable = "RECKONRY_SUPPRESS_REPOSITORY_INPUT_WARNING";
    private static readonly HashSet<string> WrittenInputSafetyWarnings = new(StringComparer.Ordinal);

    public static async Task<int> RunAsync(string[] args, AppServices services)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (args.Length == 0 || IsHelp(args))
        {
            PrintHelp();
            return ExitSuccess;
        }

        if (IsVersion(args))
        {
            PrintVersion();
            return ExitSuccess;
        }

        if (TryPrintCommandHelp(args))
        {
            return ExitSuccess;
        }

        if (args is ["importers"])
        {
            return ListImporters(services);
        }

        if (args is ["plugins"])
        {
            return ListPlugins(services);
        }

        if (args is ["import", var source, .. var importArgs])
        {
            return await ImportSourceAsync(source, importArgs, services);
        }

        if (args is ["validate", .. var validateArgs])
        {
            return await ValidateAsync(validateArgs, services);
        }

        if (args is ["config", "italy-rw-template", .. var italyRwTemplateArgs])
        {
            return await ConfigItalyRwTemplateAsync(italyRwTemplateArgs, services);
        }

        if (args is ["tax", "italy", "rw", "template", .. var italyRwTemplateArgsV2])
        {
            return await ConfigItalyRwTemplateAsync(italyRwTemplateArgsV2, services);
        }

        if (args is ["config", "italy-rw-fill-binance", .. var italyRwFillBinanceArgs])
        {
            return await ConfigItalyRwFillBinanceAsync(italyRwFillBinanceArgs, services);
        }

        if (args is ["tax", "italy", "rw", "fill", "binance", .. var italyRwFillBinanceArgsV2])
        {
            return await ConfigItalyRwFillBinanceAsync(italyRwFillBinanceArgsV2, services);
        }

        if (args is ["report", "rw-snapshot", .. var reportArgs])
        {
            return await ReportRwSnapshotAsync(reportArgs, services);
        }

        if (args is ["tax", "italy", "rw", "snapshot", .. var reportArgsV2])
        {
            return await ReportRwSnapshotAsync(reportArgsV2, services);
        }

        if (args is ["report", "rw-value", .. var valueReportArgs])
        {
            return await ReportRwValueAsync(valueReportArgs, services);
        }

        if (args is ["tax", "italy", "rw", "value", .. var valueReportArgsV2])
        {
            return await ReportRwValueAsync(valueReportArgsV2, services);
        }

        if (args is ["report", "italy-rw-accountant", .. var italyRwAccountantArgs])
        {
            return await ReportItalyRwAccountantAsync(italyRwAccountantArgs, services);
        }

        if (args is ["tax", "italy", "accountant", .. var italyRwAccountantArgsV2])
        {
            return await ReportItalyRwAccountantAsync(italyRwAccountantArgsV2, services);
        }

        if (args is ["report", "tax-dossier", .. var taxDossierArgs])
        {
            return await ReportTaxDossierAsync(taxDossierArgs, services);
        }

        if (args is ["tax", "italy", "dossier", .. var taxDossierArgsV2])
        {
            return await ReportTaxDossierAsync(taxDossierArgsV2, services);
        }

        if (args is ["reconcile", "binance", .. var reconcileArgs])
        {
            return await ReconcileProviderCountryAsync("binance", "italy", reconcileArgs, services);
        }

        if (args is ["reconcile", var provider, var country, .. var reconcileArgsV2])
        {
            return await ReconcileProviderCountryAsync(provider, country, reconcileArgsV2, services);
        }

        if (args is ["audit", .. var auditArgs])
        {
            return await AuditAsync(auditArgs, services);
        }

        if (args is ["report", "audit", .. var reportAuditArgs])
        {
            return await AuditAsync(reportAuditArgs, services);
        }

        if (args is ["report", "integrity", .. var reportIntegrityArgs])
        {
            return await AuditAsync(reportIntegrityArgs, services);
        }

        WriteError(
            $"Unknown command: {string.Join(' ', args)}",
            "reckonry <command> [options]",
            "Run `reckonry --help` to list available commands.");
        return ExitUsage;
    }

}
