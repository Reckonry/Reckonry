using Reckonry.Core;
using Reckonry.Tax.Italy.Rw;
using System.Reflection;

return await ReckonryCli.RunAsync(args, AppServices.CreateDefault());

internal static class ReckonryCli
{
    public static async Task<int> RunAsync(string[] args, AppServices services)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (args.Length == 0 || IsHelp(args))
        {
            PrintHelp();
            return 0;
        }

        if (args is ["importers"])
        {
            return ListImporters(services);
        }

        if (args is ["import", var exchange, .. var importArgs])
        {
            return await ImportExchangeAsync(exchange, importArgs, services);
        }

        if (args is ["validate", .. var validateArgs])
        {
            return await ValidateAsync(validateArgs, services);
        }

        if (args is ["config", "italy-rw-template", .. var italyRwTemplateArgs])
        {
            return await ConfigItalyRwTemplateAsync(italyRwTemplateArgs, services);
        }

        if (args is ["config", "italy-rw-fill-binance", .. var italyRwFillBinanceArgs])
        {
            return await ConfigItalyRwFillBinanceAsync(italyRwFillBinanceArgs, services);
        }

        if (args is ["report", "rw-snapshot", .. var reportArgs])
        {
            return await ReportRwSnapshotAsync(reportArgs, services);
        }

        if (args is ["report", "rw-value", .. var valueReportArgs])
        {
            return await ReportRwValueAsync(valueReportArgs, services);
        }

        if (args is ["report", "italy-rw-accountant", .. var italyRwAccountantArgs])
        {
            return await ReportItalyRwAccountantAsync(italyRwAccountantArgs, services);
        }

        if (args is ["report", "tax-dossier", .. var taxDossierArgs])
        {
            return await ReportTaxDossierAsync(taxDossierArgs, services);
        }

        if (args is ["reconcile", "binance", .. var reconcileArgs])
        {
            return await ReconcileBinanceAsync(reconcileArgs, services);
        }

        if (args is ["audit", .. var auditArgs])
        {
            return await AuditAsync(auditArgs, services);
        }

        Console.Error.WriteLine("Unknown command. Run `reckonry --help` for usage.");
        return 1;
    }

    private static async Task<int> ImportExchangeAsync(string exchange, string[] args, AppServices services)
    {
        var input = GetOption(args, "--input");
        var output = GetOption(args, "--out");

        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(output))
        {
            Console.Error.WriteLine("Missing required options. Usage: reckonry import <exchange> --input <folder> --out <ledger.json>");
            return 1;
        }

        if (!services.ImporterFactory.TryCreate(exchange, out var importer))
        {
            Console.Error.WriteLine($"No importer is registered for exchange '{exchange}'.");
            Console.Error.WriteLine("Run `reckonry importers` to list available importers.");
            return 1;
        }

        WriteInputSafetyWarning(input);

        IReadOnlyList<LedgerEvent> events;
        try
        {
            events = importer.ImportFolder(input);
        }
        catch (NotSupportedException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }

        await services.LedgerReportWriter.WriteAsync(output, events);

        Console.WriteLine($"Imported {events.Count} event(s) using {importer.Descriptor.DisplayName}.");
        Console.WriteLine($"Wrote ledger report to {output}.");
        Console.WriteLine($"Unknown events: {events.Count(e => e.EventType == LedgerEventType.Unknown)}.");
        return 0;
    }

    private static async Task<int> ReconcileBinanceAsync(string[] args, AppServices services)
    {
        var officialReports = GetOption(args, "--reports");
        var reckonryReports = GetOption(args, "--ledger-reports");
        var outputFolder = GetOption(args, "--out");

        if (string.IsNullOrWhiteSpace(officialReports)
            || string.IsNullOrWhiteSpace(reckonryReports)
            || string.IsNullOrWhiteSpace(outputFolder))
        {
            Console.Error.WriteLine("Missing required options. Usage: reckonry reconcile binance --reports <official-pdfs> --ledger-reports <reports> --out <output>");
            return 1;
        }

        WriteInputSafetyWarning(officialReports);

        var summary = await services.BinanceReconciliationEngine.ReconcileAsync(officialReports, reckonryReports, outputFolder);

        Console.WriteLine($"Wrote Binance reconciliation summary to {outputFolder}.");
        foreach (var document in summary.Documents)
        {
            Console.WriteLine(
                $"ReportType={document.ReportType}; Year={document.Year?.ToString() ?? "Unknown"}; ExtractionSucceeded={document.ExtractionSucceeded}; Fields={document.ExtractedFieldCount}; Status={document.Status}");
        }

        return 0;
    }

    private static async Task<int> AuditAsync(string[] args, AppServices services)
    {
        var input = GetOption(args, "--input");
        var outputFolder = GetOption(args, "--out");

        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(outputFolder))
        {
            Console.Error.WriteLine("Missing required options. Usage: reckonry audit --input <ledger.json> --out <output>");
            return 1;
        }

        WriteInputSafetyWarning(input);

        if (!File.Exists(input))
        {
            Console.Error.WriteLine($"Ledger file was not found: {input}");
            return 1;
        }

        var events = await services.LedgerStore.ReadAsync(input);
        var report = await services.IntegrityChecker.WriteAsync(outputFolder, events);

        Console.WriteLine($"Wrote ledger integrity report to {outputFolder}.");
        Console.WriteLine($"Integrity Score: {report.IntegrityScore}");
        Console.WriteLine($"Confidence Score: {report.ConfidenceScore}");
        Console.WriteLine($"Warnings: {report.Warnings.Count}");
        Console.WriteLine($"Recommendations: {report.Recommendations.Count}");
        return 0;
    }

    private static async Task<int> ReportRwValueAsync(string[] args, AppServices services)
    {
        var input = GetOption(args, "--input");
        var yearText = GetOption(args, "--year");
        var outputFolder = GetOption(args, "--out");

        if (string.IsNullOrWhiteSpace(input)
            || string.IsNullOrWhiteSpace(yearText)
            || string.IsNullOrWhiteSpace(outputFolder))
        {
            Console.Error.WriteLine("Missing required options. Usage: reckonry report rw-value --input <ledger.json> --year <year> --out <reports>");
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

        var events = await services.LedgerStore.ReadAsync(input);
        var rows = await services.RwValueReportWriter.WriteAsync(outputFolder, year, events);

        Console.WriteLine($"Wrote RW value report for {year} to {outputFolder}.");
        Console.WriteLine($"Assets included: {rows.Count}.");
        Console.WriteLine($"Warnings: {rows.Count(r => !string.IsNullOrWhiteSpace(r.Warning))}.");
        return 0;
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
            Console.Error.WriteLine("Missing required options. Usage: reckonry config italy-rw-template --year <year> --ledger <ledger.json> --out <config.json>");
            return 1;
        }

        WriteInputSafetyWarning(ledger);

        if (!int.TryParse(yearText, out var year) || year is < 1 or > 9999)
        {
            Console.Error.WriteLine($"Invalid year: {yearText}");
            return 1;
        }

        if (!File.Exists(ledger))
        {
            Console.Error.WriteLine($"Ledger file was not found: {ledger}");
            return 1;
        }

        var events = await services.LedgerStore.ReadAsync(ledger);
        var result = await services.ItalyRwConfigWorkflow.WriteTemplateAsync(year, events, output);
        PrintConfigWorkflowResult(result);
        return 0;
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
            Console.Error.WriteLine("Missing required options. Usage: reckonry config italy-rw-fill-binance --config <config.json> --reconciliation <reconciliation-summary.json> --out <config.json>");
            return 1;
        }

        WriteInputSafetyWarning(config);
        WriteInputSafetyWarning(reconciliation);

        if (!File.Exists(config))
        {
            Console.Error.WriteLine($"Italy RW config file was not found: {config}");
            return 1;
        }

        var result = await services.ItalyRwConfigWorkflow.FillFromBinanceAsync(config, reconciliation, output);
        PrintConfigWorkflowResult(result);
        return 0;
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
            Console.Error.WriteLine("Missing required options. Usage: reckonry report italy-rw-accountant --input <ledger.json> --year <year> --out <output> [--language it-IT|en-US]");
            return 1;
        }

        if (!TryResolveLanguage(languageOption, ReportLanguages.Italian, out var language))
        {
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
        return 0;
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
            Console.Error.WriteLine("Missing required options. Usage: reckonry report tax-dossier --year <year> --ledger <ledger.json> --handoff <accountant-handoff.json> --rw <italy-rw-accountant.json> --out <output> [--language it-IT|en-US]");
            return 1;
        }

        if (!TryResolveLanguage(languageOption, ReportLanguages.Italian, out var language))
        {
            return 1;
        }

        WriteInputSafetyWarning(ledger);
        WriteInputSafetyWarning(handoff);
        WriteInputSafetyWarning(rwReport);

        if (!int.TryParse(yearText, out var year) || year is < 1 or > 9999)
        {
            Console.Error.WriteLine($"Invalid year: {yearText}");
            return 1;
        }

        foreach (var path in new[] { ledger, handoff, rwReport })
        {
            if (!File.Exists(path))
            {
                Console.Error.WriteLine($"Required input file was not found: {path}");
                return 1;
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
        return 0;
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
            Console.Error.WriteLine(ex.Message);
            language = defaultLanguage;
            return false;
        }
    }

    private static async Task<int> ValidateAsync(string[] args, AppServices services)
    {
        var input = GetOption(args, "--input");

        if (string.IsNullOrWhiteSpace(input))
        {
            Console.Error.WriteLine("Missing required option. Usage: reckonry validate --input <ledger.json>");
            return 1;
        }

        WriteInputSafetyWarning(input);

        if (!File.Exists(input))
        {
            Console.Error.WriteLine($"Ledger file was not found: {input}");
            return 1;
        }

        var validation = await services.LedgerValidator.ValidateFileAsync(input);
        if (!validation.IsValid)
        {
            Console.WriteLine("Validation errors:");
            foreach (var error in validation.Errors)
            {
                Console.WriteLine($"- {error}");
            }

            return 1;
        }

        Console.WriteLine("PASS");
        return 0;
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
            Console.Error.WriteLine("Missing required options. Usage: reckonry report rw-snapshot --input <ledger.json> --year <year> --out <reports>");
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

        var events = await services.LedgerStore.ReadAsync(input);
        var rows = await services.RwSnapshotReportWriter.WriteAsync(outputFolder, year, events);

        Console.WriteLine($"Wrote RW snapshot report for {year} to {outputFolder}.");
        Console.WriteLine($"Assets included: {rows.Count}.");
        Console.WriteLine($"Unknown event warnings: {rows.Count(r => r.UnknownEventCount > 0)}.");
        return 0;
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
            Reckonry - crypto ledger engine

            Usage:
              reckonry --help
              reckonry importers
              reckonry import <exchange> --input <folder> --out <ledger.json>
              reckonry import binance --input <folder> --out <ledger.json>
              reckonry validate --input <ledger.json>
              reckonry config italy-rw-template --year <year> --ledger <ledger.json> --out <config.json>
              reckonry config italy-rw-fill-binance --config <config.json> --reconciliation <reconciliation-summary.json> --out <config.json>
              reckonry report rw-snapshot --input <ledger.json> --year <year> --out <reports>
              reckonry report rw-value --input <ledger.json> --year <year> --out <reports>
              reckonry report italy-rw-accountant --input <ledger.json> --year <year> --out <output> [--language it-IT|en-US]
              reckonry report tax-dossier --year <year> --ledger <ledger.json> --handoff <accountant-handoff.json> --rw <italy-rw-accountant.json> --out <output> [--language it-IT|en-US]
              reckonry reconcile binance --reports <official-pdfs> --ledger-reports <reports> --out <output>
              reckonry audit --input <ledger.json> --out <output>
            """);
    }

    private static int ListImporters(AppServices services)
    {
        Console.WriteLine("Registered exchange importers:");
        foreach (var descriptor in services.ImporterFactory.ListImporters())
        {
            Console.WriteLine(
                $"{descriptor.Id} | {descriptor.DisplayName} | Version {descriptor.ImporterVersion} | Coverage {descriptor.CoveragePercent:0.##}%");
            Console.WriteLine($"  Files: {FormatList(descriptor.SupportedFiles)}");
            Console.WriteLine($"  Schemas: {FormatList(descriptor.SupportedSchemas)}");
            Console.WriteLine($"  Operations: {FormatList(descriptor.SupportedOperations)}");
        }

        return 0;
    }

    private static string FormatList(IEnumerable<string> values)
    {
        return string.Join(", ", values.Where(v => !string.IsNullOrWhiteSpace(v)));
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
