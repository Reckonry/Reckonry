using Reckonry.Core;
using Reckonry.Importers.Abstractions;
using Reckonry.Tax.Italy.Rw;
using System.Reflection;

internal static partial class ReckonryCli
{
    private static void WriteInputSafetyWarning(string input)
    {
        if (string.Equals(Environment.GetEnvironmentVariable(SuppressRepositoryInputWarningVariable), "1", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var warning = RepositoryInputSafety.BuildTrackedFolderWarning(input, Directory.GetCurrentDirectory());
        if (!string.IsNullOrWhiteSpace(warning) && WrittenInputSafetyWarnings.Add(warning))
        {
            Console.Error.WriteLine($"Warning: {warning}");
        }
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
}
