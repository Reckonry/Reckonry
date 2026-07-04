using Reckonry.Core;
using Reckonry.Importers.Abstractions;
using Reckonry.Tax.Italy.Rw;
using System.Reflection;

internal static partial class ReckonryCli
{
    private const string Green = "\u001b[32m";
    private const string Yellow = "\u001b[33m";
    private const string Red = "\u001b[31m";
    private const string Cyan = "\u001b[36m";
    private const string Bold = "\u001b[1m";
    private const string Reset = "\u001b[0m";

    private static bool UseColor =>
        !Console.IsOutputRedirected
        && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("NO_COLOR"));

    private static bool UseErrorColor =>
        !Console.IsErrorRedirected
        && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("NO_COLOR"));

    private static string Paint(string text, string color)
    {
        return UseColor ? $"{color}{text}{Reset}" : text;
    }

    private static string PaintError(string text, string color)
    {
        return UseErrorColor ? $"{color}{text}{Reset}" : text;
    }

    private static void WriteBrand()
    {
        Console.WriteLine(Paint("RECKONRY", Bold + Cyan));
        Console.WriteLine("Reviewable digital asset ledger infrastructure.");
    }

    private static void WriteSection(string title)
    {
        Console.WriteLine();
        Console.WriteLine(Paint(title, Bold));
    }

    private static void WritePhase(string title)
    {
        Console.WriteLine(Paint($"==> {title}", Cyan));
    }

    private static void WriteSuccess(string message)
    {
        Console.WriteLine($"{Paint("OK", Green)}  {message}");
    }

    private static void WriteWarningLine(string message)
    {
        Console.WriteLine($"{Paint("WARN", Yellow)}  {message}");
    }

    private static void WriteInfo(string label, object? value)
    {
        Console.WriteLine($"  {label}: {value}");
    }

    private static void WriteNext(string command)
    {
        Console.WriteLine();
        Console.WriteLine($"{Paint("Next", Cyan)}: {command}");
    }

    private static void WriteError(string message, string? usage = null, string? hint = null)
    {
        Console.Error.WriteLine($"{PaintError("ERROR", Red)} {message}");
        if (!string.IsNullOrWhiteSpace(usage))
        {
            Console.Error.WriteLine($"Usage: {usage}");
        }

        if (!string.IsNullOrWhiteSpace(hint))
        {
            Console.Error.WriteLine($"Hint: {hint}");
        }
    }
}
