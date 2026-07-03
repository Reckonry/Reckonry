namespace Reckonry.Tests;

public sealed class ArchitectureBoundaryTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Core_HasNoCountryProviderTaxReportOrImporterReferences()
    {
        AssertForbiddenTokens(
            Path.Combine(RepositoryRoot, "src", "Reckonry.Core"),
            [
                "Italy",
                "RW",
                "Rw",
                "AgenziaEntrate",
                "Agenzia",
                "Binance",
                "Coinbase",
                "Kraken",
                "Revolut",
                "Bitstamp",
                "CryptoCom",
                "Tax",
                "Reports",
                "Importers",
                "Providers",
                "Countries"
            ]);
    }

    [Fact]
    public void GenericReports_HaveNoCountryProviderOrRwConcepts()
    {
        AssertForbiddenTokens(
            Path.Combine(RepositoryRoot, "src", "Reckonry.Reports"),
            ["Italy", "RW", "Rw", "AgenziaEntrate", "Agenzia", "Binance", "EUR"]);
    }

    [Fact]
    public void GenericReconciliationAbstractions_HaveNoProviderOrCountryConcepts()
    {
        AssertForbiddenTokens(
            Path.Combine(RepositoryRoot, "src", "Reckonry.Reconciliation.Abstractions"),
            ["Italy", "RW", "Rw", "AgenziaEntrate", "Agenzia", "Binance"]);
    }

    [Fact]
    public void GenericProjects_DoNotReferenceCountryOrProviderProjects()
    {
        var projectFiles = new[]
        {
            Path.Combine(RepositoryRoot, "src", "Reckonry.Core", "Reckonry.Core.csproj"),
            Path.Combine(RepositoryRoot, "src", "Reckonry.Reports", "Reckonry.Reports.csproj"),
            Path.Combine(RepositoryRoot, "src", "Reckonry.Reconciliation.Abstractions", "Reckonry.Reconciliation.Abstractions.csproj")
        };

        foreach (var projectFile in projectFiles)
        {
            var text = File.ReadAllText(projectFile);
            Assert.DoesNotContain("Reckonry.Tax.Italy", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Reckonry.Importers.Binance", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Reckonry.Reconciliation.Binance.Italy", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static void AssertForbiddenTokens(string folder, IReadOnlyCollection<string> tokens)
    {
        var sourceFiles = Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
            .Where(path => Path.GetExtension(path) is ".cs" or ".csproj")
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));

        foreach (var sourceFile in sourceFiles)
        {
            var text = File.ReadAllText(sourceFile);
            foreach (var token in tokens)
            {
                Assert.DoesNotContain(token, text, StringComparison.Ordinal);
            }
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Reckonry.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find Reckonry.sln from test base directory.");
    }
}
