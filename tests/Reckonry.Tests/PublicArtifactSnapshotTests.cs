using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Reckonry.Reconciliation.Binance.Italy;
using Reckonry.Tax.Italy.Rw;

namespace Reckonry.Tests;

public sealed partial class PublicArtifactSnapshotTests
{
    private const string SnapshotUpdateVariable = "RECKONRY_UPDATE_SNAPSHOTS";

    [Fact]
    public async Task CliOutput_MatchesSnapshots()
    {
        var root = FindRepositoryRoot();
        var output = new StringBuilder();

        await AppendCommandAsync(root, output, "reckonry --help", ["--help"]);
        await AppendCommandAsync(root, output, "reckonry --version", ["--version"]);
        await AppendCommandAsync(root, output, "reckonry doctor plugins", ["doctor", "plugins"]);
        await AppendCommandAsync(root, output, "reckonry plugins", ["plugins"]);
        await AppendCommandAsync(root, output, "reckonry importers", ["importers"]);
        await AppendCommandAsync(root, output, "reckonry unknown", ["unknown"], expectedExitCode: 64);

        AssertSnapshot("cli-output.snap", NormalizeSnapshotText(output.ToString(), root, null));
    }

    [Fact]
    public async Task SyntheticDemoArtifacts_MatchSnapshots()
    {
        var root = FindRepositoryRoot();
        using var demo = await GeneratedDemo.CreateAsync(root);

        AssertSnapshot("demo-output.snap", NormalizeSnapshotText(demo.Output, root, demo.WorkRoot));
        AssertSnapshot("audit-markdown.snap", NormalizeArtifact(File.ReadAllText(Path.Combine(demo.DemoRoot, "audit", "integrity.md")), root, demo.WorkRoot));
        AssertSnapshot("reconciliation-markdown.snap", NormalizeArtifact(File.ReadAllText(Path.Combine(demo.DemoRoot, "reconciliation", "reconciliation-summary.md")), root, demo.WorkRoot));
        AssertSnapshot("accountant-markdown.snap", NormalizeArtifact(File.ReadAllText(Path.Combine(demo.DemoRoot, "accountant", "italy-rw-accountant-2025.md")), root, demo.WorkRoot));
        AssertSnapshot("rw-snapshot-csv.snap", NormalizeArtifact(File.ReadAllText(Path.Combine(demo.DemoRoot, "reports", "rw-snapshot-2025.csv")), root, demo.WorkRoot));
        AssertSnapshot("rw-value-csv.snap", NormalizeArtifact(File.ReadAllText(Path.Combine(demo.DemoRoot, "reports", "rw-value-2025.csv")), root, demo.WorkRoot));
        AssertSnapshot("rw-snapshot-json.snap", NormalizeArtifact(File.ReadAllText(Path.Combine(demo.DemoRoot, "reports", "rw-snapshot-2025.json")), root, demo.WorkRoot));
        AssertSnapshot("rw-value-json.snap", NormalizeArtifact(File.ReadAllText(Path.Combine(demo.DemoRoot, "reports", "rw-value-2025.json")), root, demo.WorkRoot));
        AssertSnapshot("tax-dossier-pdf-structure.snap", BuildPdfStructureSnapshot(Path.Combine(demo.DemoRoot, "accountant", "Reckonry-Tax-Dossier-2025.pdf")));
    }

    [Fact]
    public async Task ExplainReports_TraceNumbersToLedgerEvidence()
    {
        var root = FindRepositoryRoot();
        using var demo = await GeneratedDemo.CreateAsync(root);
        var output = Path.Combine(demo.DemoRoot, "explain-rw-snapshot.md");
        var result = await RunCliAsync(
            root,
            [
                "explain",
                "--input",
                Path.Combine(demo.DemoRoot, "reports", "rw-snapshot-2025.json"),
                "--ledger",
                Path.Combine(demo.DemoRoot, "ledger.json"),
                "--out",
                output
            ],
            root);

        Assert.Equal(0, result.ExitCode);
        var explanation = await File.ReadAllTextAsync(output);
        Assert.Contains("Reckonry Explain: Italy RW snapshot report", explanation);
        Assert.Contains("Report: `", explanation);
        Assert.Contains("Ledger: `", explanation);
        Assert.Contains("`ClosingQuantity`", explanation);
        Assert.Contains("ledger event `", explanation);
        Assert.Contains("posting `", explanation);
        Assert.Contains("source row `Binance` `normalized-transactions.csv:", explanation);
        Assert.Contains("raw `", explanation);
    }

    [Fact]
    public async Task ExplainPdf_FailsInsteadOfInventingStructure()
    {
        var root = FindRepositoryRoot();
        using var demo = await GeneratedDemo.CreateAsync(root);
        var result = await RunCliAsync(
            root,
            [
                "explain",
                Path.Combine(demo.DemoRoot, "accountant", "Reckonry-Tax-Dossier-2025.pdf"),
                "--ledger",
                Path.Combine(demo.DemoRoot, "ledger.json")
            ],
            root);

        Assert.Equal(65, result.ExitCode);
        Assert.Contains("PDF explanation is not supported", result.Stderr);
    }

    [Fact]
    public void ReadmeCodeExamples_MatchSnapshot()
    {
        var root = FindRepositoryRoot();
        var readme = File.ReadAllText(Path.Combine(root, "README.md"));
        var builder = new StringBuilder();
        var index = 0;

        foreach (Match match in MarkdownFenceRegex().Matches(readme))
        {
            var language = match.Groups["language"].Value.Trim();
            if (language is not ("bash" or "powershell"))
            {
                continue;
            }

            index++;
            builder.AppendLine($"## Example {index}: {language}");
            builder.AppendLine(match.Groups["code"].Value.Trim());
            builder.AppendLine();
        }

        Assert.True(index > 0, "README.md should contain public command examples.");
        AssertSnapshot("readme-code-examples.snap", NormalizeLineEndings(builder.ToString()));
    }

    private static async Task AppendCommandAsync(
        string repositoryRoot,
        StringBuilder builder,
        string title,
        IReadOnlyList<string> args,
        int expectedExitCode = 0)
    {
        var result = await RunCliAsync(repositoryRoot, args, repositoryRoot);

        Assert.Equal(expectedExitCode, result.ExitCode);

        builder.AppendLine($"## {title}");
        builder.AppendLine("$ " + title);
        if (!string.IsNullOrWhiteSpace(result.Stdout))
        {
            builder.AppendLine(result.Stdout.TrimEnd());
        }

        if (!string.IsNullOrWhiteSpace(result.Stderr))
        {
            builder.AppendLine("[stderr]");
            builder.AppendLine(result.Stderr.TrimEnd());
        }

        builder.AppendLine();
    }

    private static async Task<CliResult> RunCliAsync(
        string repositoryRoot,
        IReadOnlyList<string> args,
        string workingDirectory)
    {
        var cliDll = Path.Combine(AppContext.BaseDirectory, "Reckonry.Cli.dll");
        Assert.True(File.Exists(cliDll), $"Built CLI was not found: {cliDll}");

        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        startInfo.Environment["RECKONRY_SUPPRESS_REPOSITORY_INPUT_WARNING"] = "1";
        startInfo.ArgumentList.Add(cliDll);
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet CLI process.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        if (!await WaitForExitAsync(process, TimeSpan.FromSeconds(30)))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"CLI command timed out: {string.Join(' ', args)}");
        }

        return new CliResult(process.ExitCode, await stdoutTask, await stderrTask);
    }

    private static async Task<bool> WaitForExitAsync(Process process, TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(cancellation.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private static void AssertSnapshot(string snapshotName, string actual)
    {
        var root = FindRepositoryRoot();
        var snapshotPath = Path.Combine(root, "tests", "Reckonry.Tests", "Snapshots", snapshotName);
        var normalizedActual = NormalizeLineEndings(actual).TrimEnd() + Environment.NewLine;

        if (ShouldUpdateSnapshots())
        {
            Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);
            File.WriteAllText(snapshotPath, normalizedActual);
            return;
        }

        Assert.True(File.Exists(snapshotPath), $"Snapshot is missing: {snapshotPath}. Run with {SnapshotUpdateVariable}=1 to create it.");

        var expected = NormalizeLineEndings(File.ReadAllText(snapshotPath)).TrimEnd() + Environment.NewLine;
        Assert.Equal(expected, normalizedActual);
    }

    private static string NormalizeSnapshotText(string text, string repositoryRoot, string? workRoot)
    {
        var normalized = NormalizeLineEndings(text)
            .Replace(repositoryRoot.Replace('\\', '/'), "<REPO>", StringComparison.Ordinal)
            .Replace(repositoryRoot, "<REPO>", StringComparison.Ordinal);

        if (!string.IsNullOrWhiteSpace(workRoot))
        {
            normalized = normalized
                .Replace(workRoot.Replace('\\', '/'), "<WORK>", StringComparison.Ordinal)
                .Replace(workRoot, "<WORK>", StringComparison.Ordinal);
        }

        return NormalizeVolatileValues(normalized);
    }

    private static string NormalizeArtifact(string text, string repositoryRoot, string? workRoot)
    {
        return NormalizeVolatileValues(NormalizeSnapshotText(text, repositoryRoot, workRoot));
    }

    private static string NormalizeVolatileValues(string text)
    {
        var normalized = GuidRegex().Replace(text, "<GUID>");
        normalized = Sha256Regex().Replace(normalized, "<SHA256>");
        normalized = GeneratedAtUtcRegex().Replace(normalized, "Generated at UTC: `<GENERATED_UTC>`");
        normalized = GeneratedUtcJsonRegex().Replace(normalized, "\"generatedUtc\": \"<GENERATED_UTC>\"");
        normalized = RuntimeLineRegex().Replace(normalized, "  Runtime: <DOTNET_RUNTIME>");
        normalized = OsLineRegex().Replace(normalized, "  OS: <OS>");
        return normalized;
    }

    private static string NormalizeLineEndings(string text)
    {
        return text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    }

    private static string BuildPdfStructureSnapshot(string pdfPath)
    {
        var extracted = new DirectPdfTextExtractor().Extract(pdfPath);
        var bytes = File.ReadAllBytes(pdfPath);
        var latin = Encoding.Latin1.GetString(bytes);
        var encrypted = latin.Contains("/Encrypt", StringComparison.Ordinal)
            || latin.Contains("/EncryptMetadata", StringComparison.Ordinal);
        var localizer = DictionaryTextLocalizer.Create(ReportLanguages.English);
        var sectionTitles = new[]
        {
            localizer.Text("Section.TableOfContents"),
            localizer.Text("Section.ExecutiveSummary"),
            localizer.Text("Section.LedgerIntegrity"),
            localizer.Text("Section.BinanceReconciliation"),
            localizer.Text("Section.SourceDocuments"),
            localizer.Text("Section.PortfolioComposition"),
            localizer.Text("Section.MovementTimeline"),
            localizer.Text("Section.RwDraft"),
            localizer.Text("Section.Rw8Draft"),
            localizer.Text("Section.ValidationMissingInputs"),
            localizer.Text("Section.AccountantChecklist"),
            localizer.Text("Section.TechnicalAppendix")
        };
        var logicalStructure = new StringBuilder();
        logicalStructure.AppendLine($"pdfVersion: {PdfVersionRegex().Match(latin).Value}");
        logicalStructure.AppendLine($"pageCount: {extracted.PageCount}");
        logicalStructure.AppendLine($"encrypted: {encrypted}");
        foreach (var sectionTitle in sectionTitles)
        {
            logicalStructure.AppendLine($"section:{sectionTitle}");
        }

        var logicalStructureHash = Convert
            .ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(logicalStructure.ToString())))
            .ToLowerInvariant();

        var builder = new StringBuilder();
        builder.AppendLine("Tax Dossier PDF Structure");
        builder.AppendLine($"pdfVersion: {PdfVersionRegex().Match(latin).Value}");
        builder.AppendLine($"pageCount: {extracted.PageCount}");
        builder.AppendLine($"encrypted: {encrypted}");
        builder.AppendLine($"logicalStructureSha256: {logicalStructureHash}");
        builder.AppendLine("logicalSectionTitles:");

        foreach (var sectionTitle in sectionTitles)
        {
            builder.AppendLine($"- {sectionTitle}");
        }

        return builder.ToString();
    }

    private static bool ShouldUpdateSnapshots()
    {
        var value = Environment.GetEnvironmentVariable(SnapshotUpdateVariable);
        return string.Equals(value, "1", StringComparison.Ordinal)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
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

        throw new InvalidOperationException("Repository root was not found.");
    }

    private sealed record CliResult(int ExitCode, string Stdout, string Stderr);

    private sealed class GeneratedDemo : IDisposable
    {
        private GeneratedDemo(string workRoot, string demoRoot, string output)
        {
            WorkRoot = workRoot;
            DemoRoot = demoRoot;
            Output = output;
        }

        public string WorkRoot { get; }

        public string DemoRoot { get; }

        public string Output { get; }

        public static async Task<GeneratedDemo> CreateAsync(string repositoryRoot)
        {
            var work = Directory.CreateTempSubdirectory("reckonry-snapshot-demo-");
            var demoRoot = Path.Combine(work.FullName, "artifacts", "demo");
            var output = new StringBuilder();
            const string year = "2025";

            try
            {
                Directory.CreateDirectory(demoRoot);

                output.AppendLine("Reckonry public demo");
                output.AppendLine("Input data is synthetic and safe to commit publicly.");
                output.AppendLine("Repository-path privacy warnings are suppressed for this synthetic demo only.");
                output.AppendLine("Expected alpha result: NOT READY FOR FILING. That means missing professional inputs are visible and not invented.");
                output.AppendLine();

                async Task RunAndAppend(params string[] args)
                {
                    var result = await RunCliAsync(repositoryRoot, args, repositoryRoot);
                    Assert.Equal(0, result.ExitCode);
                    if (!string.IsNullOrWhiteSpace(result.Stdout))
                    {
                        output.Append(result.Stdout);
                    }

                    if (!string.IsNullOrWhiteSpace(result.Stderr))
                    {
                        output.AppendLine("[stderr]");
                        output.Append(result.Stderr);
                    }
                }

                await RunAndAppend("plugins");
                await RunAndAppend("import", "binance", "--input", Path.Combine(repositoryRoot, "samples", "demo", "binance"), "--out", Path.Combine(demoRoot, "ledger.json"));
                await RunAndAppend("validate", "--input", Path.Combine(demoRoot, "ledger.json"));
                await RunAndAppend("report", "integrity", "--input", Path.Combine(demoRoot, "ledger.json"), "--out", Path.Combine(demoRoot, "audit"));
                output.AppendLine();
                output.AppendLine("Second importer platform demo: Coinbase synthetic export");
                await RunAndAppend("import", "coinbase", "--input", Path.Combine(repositoryRoot, "samples", "demo", "coinbase"), "--out", Path.Combine(demoRoot, "coinbase", "ledger.json"));
                await RunAndAppend("validate", "--input", Path.Combine(demoRoot, "coinbase", "ledger.json"));
                await RunAndAppend("report", "integrity", "--input", Path.Combine(demoRoot, "coinbase", "ledger.json"), "--out", Path.Combine(demoRoot, "coinbase", "audit"));
                await RunAndAppend("reconcile", "coinbase", "global", "--reports", Path.Combine(repositoryRoot, "samples", "demo", "coinbase-official-reports"), "--ledger-reports", Path.Combine(demoRoot, "coinbase"), "--out", Path.Combine(demoRoot, "coinbase", "reconciliation"));
                await RunAndAppend("tax", "italy", "rw", "snapshot", "--input", Path.Combine(demoRoot, "ledger.json"), "--year", year, "--out", Path.Combine(demoRoot, "reports"));
                await RunAndAppend("tax", "italy", "rw", "value", "--input", Path.Combine(demoRoot, "ledger.json"), "--year", year, "--out", Path.Combine(demoRoot, "reports"));
                await RunAndAppend("reconcile", "binance", "italy", "--reports", Path.Combine(repositoryRoot, "samples", "demo", "official-reports"), "--ledger-reports", Path.Combine(demoRoot, "reports"), "--out", Path.Combine(demoRoot, "reconciliation"));
                await RunAndAppend("tax", "italy", "rw", "template", "--year", year, "--ledger", Path.Combine(demoRoot, "ledger.json"), "--out", Path.Combine(demoRoot, "config", $"italy-rw-{year}.template.json"));
                await RunAndAppend("tax", "italy", "rw", "fill", "binance", "--config", Path.Combine(demoRoot, "config", $"italy-rw-{year}.template.json"), "--reconciliation", Path.Combine(demoRoot, "reconciliation", "reconciliation-summary.json"), "--out", Path.Combine(demoRoot, "config", $"italy-rw-{year}.binance-filled.json"));
                await RunAndAppend("tax", "italy", "accountant", "--input", Path.Combine(demoRoot, "ledger.json"), "--year", year, "--out", Path.Combine(demoRoot, "accountant"), "--language", "en-US");

                File.Copy(
                    Path.Combine(repositoryRoot, "samples", "demo", "italy-rw", $"accountant-handoff-{year}.fake.json"),
                    Path.Combine(demoRoot, "accountant", $"accountant-handoff-{year}.json"),
                    overwrite: true);

                await RunAndAppend("tax", "italy", "dossier", "--year", year, "--ledger", Path.Combine(demoRoot, "ledger.json"), "--handoff", Path.Combine(demoRoot, "accountant", $"accountant-handoff-{year}.json"), "--rw", Path.Combine(demoRoot, "accountant", $"italy-rw-accountant-{year}.json"), "--out", Path.Combine(demoRoot, "accountant"), "--language", "en-US");

                output.AppendLine();
                output.AppendLine("Demo complete. Generated outputs:");
                foreach (var file in Directory.EnumerateFiles(demoRoot, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
                {
                    output.AppendLine(Path.GetRelativePath(work.FullName, file).Replace('\\', '/'));
                }

                output.AppendLine();
                output.AppendLine("What to inspect first:");
                output.AppendLine("- artifacts/demo/ledger.json");
                output.AppendLine("- artifacts/demo/audit/integrity.md");
                output.AppendLine("- artifacts/demo/coinbase/ledger.json");
                output.AppendLine("- artifacts/demo/coinbase/audit/integrity.md");
                output.AppendLine("- artifacts/demo/coinbase/reconciliation/reconciliation-summary.md");
                output.AppendLine("- artifacts/demo/reconciliation/reconciliation-summary.md");
                output.AppendLine("- artifacts/demo/accountant/italy-rw-accountant-2025.md");
                output.AppendLine("- artifacts/demo/accountant/Reckonry-Tax-Dossier-2025.pdf");

                return new GeneratedDemo(work.FullName, demoRoot, output.ToString());
            }
            catch
            {
                work.Delete(recursive: true);
                throw;
            }
        }

        public void Dispose()
        {
            if (Directory.Exists(WorkRoot))
            {
                Directory.Delete(WorkRoot, recursive: true);
            }
        }
    }

    [GeneratedRegex(@"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b")]
    private static partial Regex GuidRegex();

    [GeneratedRegex(@"\b[0-9a-fA-F]{64}\b")]
    private static partial Regex Sha256Regex();

    [GeneratedRegex(@"Generated at UTC: `[^`]+`")]
    private static partial Regex GeneratedAtUtcRegex();

    [GeneratedRegex("\"generatedUtc\"\\s*:\\s*\"[^\"]+\"")]
    private static partial Regex GeneratedUtcJsonRegex();

    [GeneratedRegex(@"^  Runtime: .+$", RegexOptions.Multiline)]
    private static partial Regex RuntimeLineRegex();

    [GeneratedRegex(@"^  OS: .+$", RegexOptions.Multiline)]
    private static partial Regex OsLineRegex();

    [GeneratedRegex("^%PDF-\\d+\\.\\d+", RegexOptions.Multiline)]
    private static partial Regex PdfVersionRegex();

    [GeneratedRegex(@"```(?<language>[A-Za-z0-9_-]+)\n(?<code>.*?)\n```", RegexOptions.Singleline)]
    private static partial Regex MarkdownFenceRegex();
}
