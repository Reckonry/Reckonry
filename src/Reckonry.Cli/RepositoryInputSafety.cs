public static class RepositoryInputSafety
{
    private static readonly HashSet<string> SafeLocalFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "input",
        "output"
    };

    public static string? BuildTrackedFolderWarning(string inputPath, string currentDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentDirectory);

        var repositoryRoot = FindRepositoryRoot(currentDirectory);
        if (repositoryRoot is null)
        {
            return null;
        }

        var fullInputPath = Path.GetFullPath(inputPath, currentDirectory);
        var fullRepositoryRoot = EnsureTrailingSeparator(Path.GetFullPath(repositoryRoot));

        if (!fullInputPath.StartsWith(fullRepositoryRoot, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var relativePath = Path.GetRelativePath(repositoryRoot, fullInputPath);
        if (relativePath.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relativePath))
        {
            return null;
        }

        var firstSegment = relativePath
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .FirstOrDefault(segment => !string.IsNullOrWhiteSpace(segment));

        if (firstSegment is null || SafeLocalFolders.Contains(firstSegment))
        {
            return null;
        }

        return "Input path is inside this repository outside ignored input/ or output/. "
            + "Keep real source exports, ledger.json, and reports in local ignored folders and never commit private financial data.";
    }

    private static string? FindRepositoryRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(startDirectory));

        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git"))
                || File.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
    }
}
