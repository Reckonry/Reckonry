namespace Reckonry.Tests;

public sealed class RepositoryInputSafetyTests
{
    [Fact]
    public void BuildTrackedFolderWarning_WarnsForInputInsideTrackedRepositoryFolder()
    {
        var warning = RepositoryInputSafety.BuildTrackedFolderWarning(
            Path.Combine("samples", "binance"),
            FindRepositoryRoot());

        Assert.NotNull(warning);
        Assert.Contains("outside ignored input/ or output/", warning);
        Assert.False(warning.StartsWith("Warning:", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("input/binance")]
    [InlineData("output/ledger.json")]
    public void BuildTrackedFolderWarning_DoesNotWarnForIgnoredLocalDataFolders(string inputPath)
    {
        var warning = RepositoryInputSafety.BuildTrackedFolderWarning(
            inputPath,
            FindRepositoryRoot());

        Assert.Null(warning);
    }

    [Fact]
    public void BuildTrackedFolderWarning_DoesNotWarnForPathOutsideRepository()
    {
        var warning = RepositoryInputSafety.BuildTrackedFolderWarning(
            Path.Combine(Path.GetTempPath(), "reckonry-binance"),
            FindRepositoryRoot());

        Assert.Null(warning);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
