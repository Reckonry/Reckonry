namespace LedgerForge.Tax.Italy.Rw;

public static class ReportLanguages
{
    public const string Italian = "it-IT";
    public const string English = "en-US";

    public static string NormalizeOrThrow(string? language, string defaultLanguage)
    {
        var candidate = string.IsNullOrWhiteSpace(language)
            ? defaultLanguage
            : language.Trim();

        if (string.Equals(candidate, Italian, StringComparison.OrdinalIgnoreCase))
        {
            return Italian;
        }

        if (string.Equals(candidate, English, StringComparison.OrdinalIgnoreCase))
        {
            return English;
        }

        throw new ArgumentException(
            $"Unsupported language '{candidate}'. Supported languages: {Italian}, {English}.",
            nameof(language));
    }
}
