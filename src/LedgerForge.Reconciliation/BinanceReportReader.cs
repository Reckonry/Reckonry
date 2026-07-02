using System.Text.RegularExpressions;

namespace LedgerForge.Reconciliation;

public sealed partial class BinanceReportReader : IBinanceReportReader
{
    public IReadOnlyList<BinanceReportDocument> ReadFolder(string inputFolder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputFolder);

        if (!Directory.Exists(inputFolder))
        {
            throw new DirectoryNotFoundException($"Input folder was not found: {inputFolder}");
        }

        return Directory
            .EnumerateFiles(inputFolder, "*.pdf", SearchOption.AllDirectories)
            .Order()
            .Select(ReadFile)
            .ToArray();
    }

    public BinanceReportDocument ReadFile(string pdfPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pdfPath);

        var extracted = PdfTextExtractor.Extract(pdfPath);
        var reportType = DetectReportType(extracted.Text);
        var taxYear = DetectYear(extracted.Text);
        var language = DetectLanguage(extracted.Text);
        var fields = ExtractStructuredFields(extracted.Text);
        var isImageOnly = string.IsNullOrWhiteSpace(extracted.Text);
        var metadata = new BinanceReportMetadata(
            reportType,
            taxYear,
            language,
            extracted.PageCount,
            isImageOnly,
            !isImageOnly);

        return reportType switch
        {
            BinanceReportType.ItalyTaxCertification => new BinanceTaxCertification(metadata, fields),
            BinanceReportType.ItalyAnnualBalanceReport => new BinanceAnnualBalanceReport(metadata, fields),
            _ => new BinanceAnnualBalanceReport(metadata, fields)
        };
    }

    private static BinanceReportType DetectReportType(string text)
    {
        var normalized = text.ToLowerInvariant();

        if ((normalized.Contains("tax", StringComparison.Ordinal) && normalized.Contains("certification", StringComparison.Ordinal))
            || normalized.Contains("certificazione fiscale", StringComparison.Ordinal)
            || normalized.Contains("certificazione", StringComparison.Ordinal))
        {
            return BinanceReportType.ItalyTaxCertification;
        }

        if ((normalized.Contains("annual", StringComparison.Ordinal) && normalized.Contains("balance", StringComparison.Ordinal))
            || normalized.Contains("balance report", StringComparison.Ordinal)
            || normalized.Contains("saldo", StringComparison.Ordinal)
            || normalized.Contains("bilancio", StringComparison.Ordinal))
        {
            return BinanceReportType.ItalyAnnualBalanceReport;
        }

        return BinanceReportType.Unknown;
    }

    private static int? DetectYear(string text)
    {
        var years = YearRegex()
            .Matches(text)
            .Select(m => int.Parse(m.Value))
            .Where(y => y is >= 2009 and <= 2100)
            .GroupBy(y => y)
            .OrderByDescending(g => g.Count())
            .ThenByDescending(g => g.Key)
            .Select(g => (int?)g.Key)
            .FirstOrDefault();

        return years;
    }

    private static string DetectLanguage(string text)
    {
        var normalized = text.ToLowerInvariant();
        var italianScore = CountAny(normalized, "certificazione", "fiscale", "imposta", "anno", "saldo", "valore");
        var englishScore = CountAny(normalized, "certification", "tax", "annual", "balance", "year", "value");

        if (italianScore == 0 && englishScore == 0)
        {
            return "Unknown";
        }

        return italianScore >= englishScore ? "it" : "en";
    }

    private static IReadOnlyDictionary<string, string> ExtractStructuredFields(string text)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in KeyValueRegex().Matches(text))
        {
            var key = NormalizeKey(match.Groups[1].Value);
            var value = match.Groups[2].Value.Trim();

            if (key.Length is < 3 or > 80 || value.Length is < 1 or > 160)
            {
                continue;
            }

            fields.TryAdd(key, value);
        }

        var annualRows = AnnualRowRegex().Matches(text).Count;
        if (annualRows > 0)
        {
            fields["DetectedAnnualBalanceRows"] = annualRows.ToString();
        }

        var eurValues = EurValueRegex().Matches(text).Count;
        if (eurValues > 0)
        {
            fields["DetectedEurValues"] = eurValues.ToString();
        }

        return fields;
    }

    private static string NormalizeKey(string value)
    {
        return Regex.Replace(value.Trim(), @"\s+", " ");
    }

    private static int CountAny(string text, params string[] tokens)
    {
        return tokens.Count(token => text.Contains(token, StringComparison.Ordinal));
    }

    [GeneratedRegex(@"\b20\d{2}\b")]
    private static partial Regex YearRegex();

    [GeneratedRegex(@"([A-Za-zÀ-ÿ][A-Za-zÀ-ÿ0-9 /()._-]{2,80})\s*:\s*([^:]{1,160}?)(?=\s+[A-Za-zÀ-ÿ][A-Za-zÀ-ÿ0-9 /()._-]{2,80}\s*:|$)")]
    private static partial Regex KeyValueRegex();

    [GeneratedRegex(@"\b[A-Z0-9]{2,12}\b\s+[-+]?\d")]
    private static partial Regex AnnualRowRegex();

    [GeneratedRegex(@"(?:€|EUR)\s*[-+]?\d|[-+]?\d[\d.,]*\s*(?:€|EUR)", RegexOptions.IgnoreCase)]
    private static partial Regex EurValueRegex();
}
