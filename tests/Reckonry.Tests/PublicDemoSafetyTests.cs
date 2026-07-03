using System.Text.RegularExpressions;

namespace Reckonry.Tests;

public sealed partial class PublicDemoSafetyTests
{
    [Fact]
    public void DemoScripts_Exist()
    {
        var root = FindRepositoryRoot();

        Assert.True(File.Exists(Path.Combine(root, "scripts", "demo.sh")));
        Assert.True(File.Exists(Path.Combine(root, "scripts", "demo.ps1")));
    }

    [Fact]
    public void DemoScripts_UseSamplesDemoAndArtifactsDemo()
    {
        var root = FindRepositoryRoot();
        var scriptPaths = new[]
        {
            Path.Combine(root, "scripts", "demo.sh"),
            Path.Combine(root, "scripts", "demo.ps1")
        };

        foreach (var scriptPath in scriptPaths)
        {
            var script = File.ReadAllText(scriptPath);

            Assert.Contains("samples/demo", script.Replace('\\', '/'));
            Assert.Contains("artifacts/demo", script.Replace('\\', '/'));
            Assert.DoesNotContain("input/", script.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("output/", script.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void DemoSamples_AreUnderSamplesDemo()
    {
        var root = FindRepositoryRoot();
        var demoRoot = Path.Combine(root, "samples", "demo");

        Assert.True(Directory.Exists(demoRoot));
        Assert.NotEmpty(Directory.EnumerateFiles(demoRoot, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public void DemoSamples_DoNotContainRealLookingSecretsWalletsOrPrivateFolders()
    {
        var root = FindRepositoryRoot();
        var demoRoot = Path.Combine(root, "samples", "demo");

        foreach (var file in Directory.EnumerateFiles(demoRoot, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(demoRoot, file).Replace('\\', '/');
            var segments = relative.Split('/', StringSplitOptions.RemoveEmptyEntries);

            Assert.DoesNotContain("input", segments, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("output", segments, StringComparer.OrdinalIgnoreCase);

            var text = File.ReadAllText(file);

            Assert.False(EvmAddressRegex().IsMatch(text), $"{relative} contains an Ethereum-address-shaped value.");
            Assert.False(BitcoinAddressRegex().IsMatch(text), $"{relative} contains a Bitcoin-address-shaped value.");
            Assert.False(WifPrivateKeyRegex().IsMatch(text), $"{relative} contains a WIF-private-key-shaped value.");
            Assert.False(ExtendedPrivateKeyRegex().IsMatch(text), $"{relative} contains an extended-private-key-shaped value.");
            Assert.DoesNotContain("BEGIN PRIVATE KEY", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("BEGIN RSA PRIVATE KEY", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("BEGIN EC PRIVATE KEY", text, StringComparison.OrdinalIgnoreCase);
        }
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

    [GeneratedRegex(@"\b0x[a-fA-F0-9]{40}\b")]
    private static partial Regex EvmAddressRegex();

    [GeneratedRegex(@"\b(?:bc1|[13])[a-zA-HJ-NP-Z0-9]{25,62}\b", RegexOptions.IgnoreCase)]
    private static partial Regex BitcoinAddressRegex();

    [GeneratedRegex(@"\b(?:5[HJK][1-9A-HJ-NP-Za-km-z]{49}|[KL][1-9A-HJ-NP-Za-km-z]{51})\b")]
    private static partial Regex WifPrivateKeyRegex();

    [GeneratedRegex(@"\b[xyz]prv[1-9A-HJ-NP-Za-km-z]{80,}\b", RegexOptions.IgnoreCase)]
    private static partial Regex ExtendedPrivateKeyRegex();
}
