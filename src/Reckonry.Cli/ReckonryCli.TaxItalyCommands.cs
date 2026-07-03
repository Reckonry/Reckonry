using Reckonry.Core;
using Reckonry.Importers.Abstractions;
using Reckonry.Tax.Italy.Rw;
using System.Reflection;

internal static partial class ReckonryCli
{
    private static async Task<int> ReportRwValueAsync(string[] args, AppServices services)
    {
        var input = GetOption(args, "--input");
        var yearText = GetOption(args, "--year");
        var outputFolder = GetOption(args, "--out");

        if (string.IsNullOrWhiteSpace(input)
            || string.IsNullOrWhiteSpace(yearText)
            || string.IsNullOrWhiteSpace(outputFolder))
        {
            WriteError(
                "Missing required options: --input, --year, and --out.",
                "reckonry tax italy rw value --input <ledger.json> --year <year> --out <reports>");
            return ExitUsage;
        }

        WriteInputSafetyWarning(input);

        if (!int.TryParse(yearText, out var year) || year is < 1 or > 9999)
        {
            WriteInvalidYear(yearText);
            return ExitUsage;
        }

        if (!File.Exists(input))
        {
            WriteError($"Ledger file was not found: {input}");
            return ExitNoInput;
        }

        var events = await services.LedgerStore.ReadAsync(input);
        var rows = await services.RwValueReportWriter.WriteAsync(outputFolder, year, events);

        Console.WriteLine($"Wrote RW value report for {year} to {outputFolder}.");
        Console.WriteLine($"Assets included: {rows.Count}.");
        Console.WriteLine($"Warnings: {rows.Count(r => !string.IsNullOrWhiteSpace(r.Warning))}.");
        return ExitSuccess;
    }

    private static async Task<int> ConfigItalyRwTemplateAsync(string[] args, AppServices services)
    {
        var yearText = GetOption(args, "--year");
        var ledger = GetOption(args, "--ledger");
        var output = GetOption(args, "--out");

        if (string.IsNullOrWhiteSpace(yearText)
            || string.IsNullOrWhiteSpace(ledger)
            || string.IsNullOrWhiteSpace(output))
        {
            WriteError(
                "Missing required options: --year, --ledger, and --out.",
                "reckonry tax italy rw template --year <year> --ledger <ledger.json> --out <config.json>");
            return ExitUsage;
        }

        WriteInputSafetyWarning(ledger);

        if (!int.TryParse(yearText, out var year) || year is < 1 or > 9999)
        {
            WriteInvalidYear(yearText);
            return ExitUsage;
        }

        if (!File.Exists(ledger))
        {
            WriteError($"Ledger file was not found: {ledger}");
            return ExitNoInput;
        }

        var events = await services.LedgerStore.ReadAsync(ledger);
        var result = await services.ItalyRwConfigWorkflow.WriteTemplateAsync(year, events, output);
        PrintConfigWorkflowResult(result);
        return ExitSuccess;
    }

    private static async Task<int> ConfigItalyRwFillBinanceAsync(string[] args, AppServices services)
    {
        var config = GetOption(args, "--config");
        var reconciliation = GetOption(args, "--reconciliation");
        var output = GetOption(args, "--out");

        if (string.IsNullOrWhiteSpace(config)
            || string.IsNullOrWhiteSpace(reconciliation)
            || string.IsNullOrWhiteSpace(output))
        {
            WriteError(
                "Missing required options: --config, --reconciliation, and --out.",
                "reckonry tax italy rw fill binance --config <config.json> --reconciliation <reconciliation-summary.json> --out <config.json>");
            return ExitUsage;
        }

        WriteInputSafetyWarning(config);
        WriteInputSafetyWarning(reconciliation);

        if (!File.Exists(config))
        {
            WriteError($"Italy RW config file was not found: {config}");
            return ExitNoInput;
        }

        var result = await services.ItalyRwConfigWorkflow.FillFromBinanceAsync(config, reconciliation, output);
        PrintConfigWorkflowResult(result);
        return ExitSuccess;
    }

    private static void PrintConfigWorkflowResult(ItalyRwConfigWorkflowResult result)
    {
        Console.WriteLine("Generated config file:");
        Console.WriteLine($"- {result.GeneratedFileName}");
        Console.WriteLine($"Total assets: {result.TotalAssets}");
        Console.WriteLine($"Filled valuation count: {result.FilledValuationCount}");
        Console.WriteLine($"Remaining missing valuation count: {result.RemainingMissingValuationCount}");
        Console.WriteLine($"Warnings: {result.WarningCount}");
    }

    private static async Task<int> ReportItalyRwAccountantAsync(string[] args, AppServices services)
    {
        var input = GetOption(args, "--input");
        var yearText = GetOption(args, "--year");
        var outputFolder = GetOption(args, "--out");
        var languageOption = GetOption(args, "--language");

        if (string.IsNullOrWhiteSpace(input)
            || string.IsNullOrWhiteSpace(yearText)
            || string.IsNullOrWhiteSpace(outputFolder))
        {
            WriteError(
                "Missing required options: --input, --year, and --out.",
                "reckonry tax italy accountant --input <ledger.json> --year <year> --out <output> [--language it-IT|en-US]");
            return ExitUsage;
        }

        if (!TryResolveLanguage(languageOption, ReportLanguages.Italian, out var language))
        {
            return ExitUsage;
        }

        WriteInputSafetyWarning(input);

        if (!int.TryParse(yearText, out var year) || year is < 1 or > 9999)
        {
            WriteInvalidYear(yearText);
            return ExitUsage;
        }

        if (!File.Exists(input))
        {
            WriteError($"Ledger file was not found: {input}");
            return ExitNoInput;
        }

        var events = await services.LedgerStore.ReadAsync(input);
        var result = await services.ItalyRwAccountantPackageWriter.WriteAsync(input, outputFolder, year, events, language);

        Console.WriteLine("Generated accountant review package files:");
        foreach (var fileName in result.GeneratedFileNames)
        {
            Console.WriteLine($"- {fileName}");
        }

        Console.WriteLine($"Readiness: {result.ReadinessStatus}");
        Console.WriteLine($"Missing inputs: {result.MissingInputCount}");
        Console.WriteLine($"Warnings: {result.WarningCount}");
        return ExitSuccess;
    }

    private static async Task<int> ReportTaxDossierAsync(string[] args, AppServices services)
    {
        var yearText = GetOption(args, "--year");
        var ledger = GetOption(args, "--ledger");
        var handoff = GetOption(args, "--handoff");
        var rwReport = GetOption(args, "--rw");
        var outputFolder = GetOption(args, "--out");
        var logo = GetOption(args, "--logo") ?? Path.Combine("assets", "logo-dark.svg");
        var languageOption = GetOption(args, "--language");

        if (string.IsNullOrWhiteSpace(yearText)
            || string.IsNullOrWhiteSpace(ledger)
            || string.IsNullOrWhiteSpace(handoff)
            || string.IsNullOrWhiteSpace(rwReport)
            || string.IsNullOrWhiteSpace(outputFolder))
        {
            WriteError(
                "Missing required options: --year, --ledger, --handoff, --rw, and --out.",
                "reckonry tax italy dossier --year <year> --ledger <ledger.json> --handoff <accountant-handoff.json> --rw <italy-rw-accountant.json> --out <output> [--language it-IT|en-US]");
            return ExitUsage;
        }

        if (!TryResolveLanguage(languageOption, ReportLanguages.Italian, out var language))
        {
            return ExitUsage;
        }

        WriteInputSafetyWarning(ledger);
        WriteInputSafetyWarning(handoff);
        WriteInputSafetyWarning(rwReport);

        if (!int.TryParse(yearText, out var year) || year is < 1 or > 9999)
        {
            WriteInvalidYear(yearText);
            return ExitUsage;
        }

        foreach (var path in new[] { ledger, handoff, rwReport })
        {
            if (!File.Exists(path))
            {
                WriteError($"Required input file was not found: {path}");
                return ExitNoInput;
            }
        }

        var result = await services.TaxDossierPdfGenerator.GenerateAsync(new TaxDossierPdfRequest(
            year,
            ledger,
            handoff,
            rwReport,
            outputFolder,
            logo,
            ReadGitCommit(),
            Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown",
            Language: language));

        Console.WriteLine("Generated tax dossier:");
        Console.WriteLine($"- {result.GeneratedFileName}");
        Console.WriteLine($"Language: {result.Language}");
        Console.WriteLine($"Readiness: {result.ReadinessStatus}");
        Console.WriteLine($"Source files: {result.SourceFileCount}");
        Console.WriteLine($"Imported rows: {result.ImportedRowCount}");
        Console.WriteLine($"Ledger events: {result.LedgerEventCount}");
        Console.WriteLine($"Unknown events: {result.UnknownEventCount}");
        Console.WriteLine($"Official report documents: {result.OfficialReportDocumentCount}");
        Console.WriteLine($"Missing valuation evidence: {result.MissingValuationEvidenceCount}");
        Console.WriteLine($"Validation errors: {result.ValidationErrorCount}");
        Console.WriteLine($"Warnings: {result.WarningCount}");
        return ExitSuccess;
    }

    private static bool TryResolveLanguage(
        string? languageOption,
        string defaultLanguage,
        out string language)
    {
        try
        {
            language = ReportLanguages.NormalizeOrThrow(languageOption, defaultLanguage);
            return true;
        }
        catch (ArgumentException ex)
        {
            WriteError(ex.Message, hint: "Use `--language it-IT` or `--language en-US`.");
            language = defaultLanguage;
            return false;
        }
    }

    private static async Task<int> ReportRwSnapshotAsync(string[] args, AppServices services)
    {
        var input = GetOption(args, "--input");
        var yearText = GetOption(args, "--year");
        var outputFolder = GetOption(args, "--out");

        if (string.IsNullOrWhiteSpace(input)
            || string.IsNullOrWhiteSpace(yearText)
            || string.IsNullOrWhiteSpace(outputFolder))
        {
            WriteError(
                "Missing required options: --input, --year, and --out.",
                "reckonry tax italy rw snapshot --input <ledger.json> --year <year> --out <reports>");
            return ExitUsage;
        }

        WriteInputSafetyWarning(input);

        if (!int.TryParse(yearText, out var year) || year is < 1 or > 9999)
        {
            WriteInvalidYear(yearText);
            return ExitUsage;
        }

        if (!File.Exists(input))
        {
            WriteError($"Ledger file was not found: {input}");
            return ExitNoInput;
        }

        var events = await services.LedgerStore.ReadAsync(input);
        var rows = await services.RwSnapshotReportWriter.WriteAsync(outputFolder, year, events);

        Console.WriteLine($"Wrote RW snapshot report for {year} to {outputFolder}.");
        Console.WriteLine($"Assets included: {rows.Count}.");
        Console.WriteLine($"Unknown event warnings: {rows.Count(r => r.UnknownEventCount > 0)}.");
        return ExitSuccess;
    }

    private static string ReadGitCommit()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            var gitDirectory = Path.Combine(directory.FullName, ".git");
            if (Directory.Exists(gitDirectory))
            {
                var headPath = Path.Combine(gitDirectory, "HEAD");
                if (!File.Exists(headPath))
                {
                    return "Unknown";
                }

                var head = File.ReadAllText(headPath).Trim();
                if (!head.StartsWith("ref:", StringComparison.OrdinalIgnoreCase))
                {
                    return ShortCommit(head);
                }

                var refPath = Path.Combine(gitDirectory, head["ref:".Length..].Trim());
                return File.Exists(refPath) ? ShortCommit(File.ReadAllText(refPath).Trim()) : "Unknown";
            }

            directory = directory.Parent;
        }

        return "Unknown";
    }

    private static string ShortCommit(string commit)
    {
        return commit.Length <= 12 ? commit : commit[..12];
    }
}
