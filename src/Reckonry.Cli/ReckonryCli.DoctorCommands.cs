using Reckonry.Core;
using Reckonry.Importers.Abstractions;
using Reckonry.Tax.Italy.Rw;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

internal static partial class ReckonryCli
{
    private static readonly string[] RequiredTemplateShortNames =
    [
        "reckonry-importer",
        "reckonry-tax-module",
        "reckonry-report",
        "reckonry-reconciliation"
    ];

    private static readonly string[] RequiredIgnoredPaths =
    [
        "input/",
        "output/",
        "artifacts/",
        ".env"
    ];

    private static int Doctor(string[] args, AppServices services)
    {
        if (args.Length > 1)
        {
            WriteError(
                $"Unknown doctor scope: {string.Join(' ', args)}",
                "reckonry doctor [plugins|privacy|environment|sdk|demo|repository]",
                "Run `reckonry doctor --help` for examples.");
            return ExitUsage;
        }

        var scope = args.Length == 0 ? "all" : args[0].ToLowerInvariant();
        var root = FindRepositoryRoot();
        var results = new List<DoctorCheck>();

        WriteBrand();
        WriteSection(scope == "all" ? "Doctor" : $"Doctor: {scope}");

        switch (scope)
        {
            case "all":
                RunEnvironmentDoctor(results);
                RunPluginDoctor(results, services);
                RunRepositoryDoctor(results, root);
                RunPrivacyDoctor(results, root);
                RunSdkDoctor(results, root);
                RunDemoDoctor(results, root);
                break;
            case "plugins":
                RunPluginDoctor(results, services);
                break;
            case "privacy":
                RunPrivacyDoctor(results, root);
                break;
            case "environment":
                RunEnvironmentDoctor(results);
                break;
            case "sdk":
                RunSdkDoctor(results, root);
                break;
            case "demo":
                RunDemoDoctor(results, root);
                break;
            case "repository":
                RunRepositoryDoctor(results, root);
                break;
            default:
                WriteError(
                    $"Unknown doctor scope: {scope}",
                    "reckonry doctor [plugins|privacy|environment|sdk|demo|repository]");
                return ExitUsage;
        }

        WriteDoctorSummary(results);
        WriteNext(scope == "all" ? "scripts/demo.sh" : "reckonry doctor");
        return results.Any(result => result.Status == DoctorStatus.Fail) ? ExitDataError : ExitSuccess;
    }

    private static void RunEnvironmentDoctor(List<DoctorCheck> results)
    {
        WritePhase("Environment");
        AddPass(results, "CLI runtime", $".NET {Environment.Version}");
        AddPass(results, "Operating system", Environment.OSVersion.ToString());
        AddPass(results, "Working directory", Directory.GetCurrentDirectory());

        var dotnetVersion = TryRunProcess("dotnet", "--version");
        if (dotnetVersion.ExitCode == 0 && !string.IsNullOrWhiteSpace(dotnetVersion.Stdout))
        {
            AddPass(results, "dotnet SDK", dotnetVersion.Stdout.Trim());
        }
        else
        {
            AddFail(results, "dotnet SDK", "dotnet --version did not complete.");
        }
    }

    private static void RunPluginDoctor(List<DoctorCheck> results, AppServices services)
    {
        WritePhase("Plugins");

        AddCount(results, "Installed importers", services.ImporterFactory.ListImporters().Count(d => !IsPlaceholderImporter(d)));
        AddCount(results, "Installed country modules", services.TaxModules.Count);
        AddCount(results, "Installed reports", services.Reports.Count);
        AddCount(results, "Installed reconciliation modules", services.ReconciliationModules.Count);
        AddCount(results, "Installed pricing providers", services.Plugins.PricingProviders.Count);

        var placeholders = services.ImporterFactory.ListImporters().Count(IsPlaceholderImporter);
        if (placeholders > 0)
        {
            AddWarn(results, "Placeholder importers", $"{placeholders} planned importer(s) are visible but not supported.");
        }
        else
        {
            AddPass(results, "Placeholder importers", "None.");
        }
    }

    private static void RunRepositoryDoctor(List<DoctorCheck> results, string root)
    {
        WritePhase("Repository");

        AddFileCheck(results, root, "Solution", "Reckonry.sln");
        AddFileCheck(results, root, "README", "README.md");
        AddFileCheck(results, root, "Git ignore", ".gitignore");
        AddFolderCheck(results, root, "Source folder", "src");
        AddFolderCheck(results, root, "Tests folder", "tests");
        AddFolderCheck(results, root, "Samples folder", "samples");
        AddFolderCheck(results, root, "Templates folder", "templates");

        var gitFolder = Path.Combine(root, ".git");
        if (Directory.Exists(gitFolder))
        {
            AddPass(results, "Git repository", ".git directory found.");
        }
        else
        {
            AddWarn(results, "Git repository", "No .git directory found from current path.");
        }

        var gitignorePath = Path.Combine(root, ".gitignore");
        var gitignore = File.Exists(gitignorePath) ? File.ReadAllText(gitignorePath) : string.Empty;
        foreach (var ignoredPath in RequiredIgnoredPaths)
        {
            if (gitignore.Contains(ignoredPath, StringComparison.OrdinalIgnoreCase))
            {
                AddPass(results, $"Ignored path {ignoredPath}", "Protected by .gitignore.");
            }
            else
            {
                AddFail(results, $"Ignored path {ignoredPath}", "Missing from .gitignore.");
            }
        }

        var artifactsFolder = Path.Combine(root, "artifacts", "doctor");
        try
        {
            Directory.CreateDirectory(artifactsFolder);
            var probe = Path.Combine(artifactsFolder, ".write-test");
            File.WriteAllText(probe, "doctor");
            File.Delete(probe);
            AddPass(results, "Artifact write permission", "artifacts/doctor is writable.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AddFail(results, "Artifact write permission", ex.Message);
        }
    }

    private static void RunPrivacyDoctor(List<DoctorCheck> results, string root)
    {
        WritePhase("Privacy");

        var samplesRoot = Path.Combine(root, "samples");
        if (!Directory.Exists(samplesRoot))
        {
            AddFail(results, "Samples folder", "samples/ was not found.");
            return;
        }

        var suspicious = Directory
            .EnumerateFiles(samplesRoot, "*", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutputPath(path))
            .SelectMany(path => ScanForPrivateLookingText(path))
            .Take(10)
            .ToArray();

        if (suspicious.Length == 0)
        {
            AddPass(results, "Synthetic samples", "No private-looking tokens found in samples/.");
        }
        else
        {
            AddFail(results, "Synthetic samples", $"{suspicious.Length} suspicious token(s) found. Review samples before publishing.");
        }

        var privateFolders = new[] { "input", "output" };
        foreach (var folder in privateFolders)
        {
            var path = Path.Combine(root, folder);
            if (Directory.Exists(path))
            {
                AddWarn(results, $"{folder}/ folder", "Folder exists locally. It should remain ignored and unstaged.");
            }
            else
            {
                AddPass(results, $"{folder}/ folder", "Not present in repository root.");
            }
        }
    }

    private static void RunSdkDoctor(List<DoctorCheck> results, string root)
    {
        WritePhase("SDK");

        AddFileCheck(results, root, "SDK template guide", Path.Combine("docs", "sdk", "plugin-template.md"));
        foreach (var shortName in RequiredTemplateShortNames)
        {
            var templateRoot = Path.Combine(root, "templates", shortName);
            var templateJson = Path.Combine(templateRoot, ".template.config", "template.json");
            var readme = Path.Combine(templateRoot, "README.md");
            var architecture = Path.Combine(templateRoot, "ARCHITECTURE.md");
            var tests = Path.Combine(templateRoot, "tests");

            if (File.Exists(templateJson) && File.Exists(readme) && File.Exists(architecture) && Directory.Exists(tests))
            {
                AddPass(results, shortName, "Template metadata, README, architecture notes, and tests are present.");
            }
            else
            {
                AddFail(results, shortName, "Template is missing required SDK files.");
            }
        }
    }

    private static void RunDemoDoctor(List<DoctorCheck> results, string root)
    {
        WritePhase("Demo");

        AddFolderCheck(results, root, "Demo samples", Path.Combine("samples", "demo"));
        AddFileCheck(results, root, "Unix demo script", Path.Combine("scripts", "demo.sh"));
        AddFileCheck(results, root, "PowerShell demo script", Path.Combine("scripts", "demo.ps1"));

        var binanceDemo = Path.Combine(root, "samples", "demo", "binance");
        var coinbaseDemo = Path.Combine(root, "samples", "demo", "coinbase");
        if (Directory.Exists(binanceDemo) || Directory.Exists(coinbaseDemo))
        {
            AddPass(results, "Provider demo data", "At least one provider demo dataset is present.");
        }
        else
        {
            AddFail(results, "Provider demo data", "No provider demo dataset found under samples/demo/.");
        }

        var artifactsDemo = Path.Combine(root, "artifacts", "demo");
        if (Directory.Exists(artifactsDemo))
        {
            AddPass(results, "Demo artifacts", "artifacts/demo exists from a previous run.");
        }
        else
        {
            AddWarn(results, "Demo artifacts", "artifacts/demo does not exist yet. Run scripts/demo.sh or scripts/demo.ps1.");
        }
    }

    private static void AddFileCheck(List<DoctorCheck> results, string root, string name, string relativePath)
    {
        var path = Path.Combine(root, relativePath);
        if (File.Exists(path))
        {
            AddPass(results, name, relativePath);
        }
        else
        {
            AddFail(results, name, $"{relativePath} was not found.");
        }
    }

    private static void AddFolderCheck(List<DoctorCheck> results, string root, string name, string relativePath)
    {
        var path = Path.Combine(root, relativePath);
        if (Directory.Exists(path))
        {
            AddPass(results, name, relativePath);
        }
        else
        {
            AddFail(results, name, $"{relativePath} was not found.");
        }
    }

    private static void AddCount(List<DoctorCheck> results, string name, int count)
    {
        if (count > 0)
        {
            AddPass(results, name, count.ToString());
        }
        else
        {
            AddWarn(results, name, "None installed.");
        }
    }

    private static void AddPass(List<DoctorCheck> results, string name, string detail)
    {
        results.Add(new(DoctorStatus.Pass, name, detail));
        WriteSuccess($"{name}: {detail}");
    }

    private static void AddWarn(List<DoctorCheck> results, string name, string detail)
    {
        results.Add(new(DoctorStatus.Warn, name, detail));
        WriteWarningLine($"{name}: {detail}");
    }

    private static void AddFail(List<DoctorCheck> results, string name, string detail)
    {
        results.Add(new(DoctorStatus.Fail, name, detail));
        Console.WriteLine($"{Paint("FAIL", Red)} {name}: {detail}");
    }

    private static void WriteDoctorSummary(IReadOnlyCollection<DoctorCheck> results)
    {
        var passed = results.Count(result => result.Status == DoctorStatus.Pass);
        var warned = results.Count(result => result.Status == DoctorStatus.Warn);
        var failed = results.Count(result => result.Status == DoctorStatus.Fail);

        WriteSection("Summary");
        WriteInfo("Passed", passed);
        WriteInfo("Warnings", warned);
        WriteInfo("Failures", failed);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Reckonry.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    private static IEnumerable<string> ScanForPrivateLookingText(string path)
    {
        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DecoderFallbackException)
        {
            yield break;
        }

        var patterns = new Dictionary<string, Regex>
        {
            ["private key"] = new("BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY", RegexOptions.IgnoreCase),
            ["seed phrase"] = new(@"\b(seed phrase|mnemonic)\b", RegexOptions.IgnoreCase),
            ["exchange secret"] = new(@"\b(api[_-]?secret|api[_-]?key|password)\b", RegexOptions.IgnoreCase),
            ["wallet address"] = new(@"\b0x[a-fA-F0-9]{40}\b", RegexOptions.IgnoreCase)
        };

        foreach (var (name, regex) in patterns)
        {
            if (regex.IsMatch(text))
            {
                yield return $"{Path.GetFileName(path)}:{name}";
            }
        }
    }

    private static bool IsBuildOutputPath(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase);
    }

    private static ProcessResult TryRunProcess(string fileName, string arguments)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo(fileName, arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });

            if (process is null)
            {
                return new(-1, string.Empty, "Process did not start.");
            }

            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit(5000);
            return new(process.ExitCode, stdout, stderr);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or System.ComponentModel.Win32Exception)
        {
            return new(-1, string.Empty, ex.Message);
        }
    }

    private enum DoctorStatus
    {
        Pass,
        Warn,
        Fail
    }

    private sealed record DoctorCheck(DoctorStatus Status, string Name, string Detail);

    private sealed record ProcessResult(int ExitCode, string Stdout, string Stderr);
}
