using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace LedgerForge.Reconciliation;

internal static partial class PdfTextExtractor
{
    public static ExtractedPdfText Extract(string pdfPath)
    {
        var bytes = File.ReadAllBytes(pdfPath);
        var latinText = Encoding.Latin1.GetString(bytes);
        var pageCount = PageRegex().Matches(latinText).Count;
        var chunks = new List<string>();

        foreach (Match streamMatch in StreamRegex().Matches(latinText))
        {
            var streamBytes = Encoding.Latin1.GetBytes(streamMatch.Groups[1].Value.Trim('\r', '\n'));

            foreach (var candidate in EnumerateStreamCandidates(streamBytes))
            {
                chunks.AddRange(ExtractTextChunks(candidate));
            }
        }

        var text = NormalizeText(string.Join(Environment.NewLine, chunks));
        return new ExtractedPdfText(pageCount, text);
    }

    private static IEnumerable<byte[]> EnumerateStreamCandidates(byte[] streamBytes)
    {
        var candidates = new List<byte[]> { streamBytes };

        try
        {
            using var input = new MemoryStream(streamBytes);
            using var deflate = new ZLibStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            deflate.CopyTo(output);
            candidates.Add(output.ToArray());
        }
        catch (InvalidDataException)
        {
        }

        return candidates;
    }

    private static IEnumerable<string> ExtractTextChunks(byte[] streamBytes)
    {
        var content = Encoding.Latin1.GetString(streamBytes);

        foreach (Match match in LiteralStringRegex().Matches(content))
        {
            var decoded = DecodePdfLiteralString(match.Groups[1].Value);
            if (!string.IsNullOrWhiteSpace(decoded))
            {
                yield return decoded;
            }
        }

        foreach (Match match in HexStringRegex().Matches(content))
        {
            var decoded = DecodePdfHexString(match.Groups[1].Value);
            if (!string.IsNullOrWhiteSpace(decoded))
            {
                yield return decoded;
            }
        }
    }

    private static string DecodePdfLiteralString(string value)
    {
        var builder = new StringBuilder(value.Length);

        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];
            if (current != '\\' || i + 1 >= value.Length)
            {
                builder.Append(current);
                continue;
            }

            var escaped = value[++i];
            builder.Append(escaped switch
            {
                'n' => '\n',
                'r' => '\r',
                't' => '\t',
                'b' => '\b',
                'f' => '\f',
                '(' => '(',
                ')' => ')',
                '\\' => '\\',
                _ => escaped
            });
        }

        return builder.ToString();
    }

    private static string DecodePdfHexString(string value)
    {
        var normalized = new string(value.Where(Uri.IsHexDigit).ToArray());
        if (normalized.Length < 2)
        {
            return string.Empty;
        }

        if (normalized.Length % 2 != 0)
        {
            normalized += "0";
        }

        var bytes = new byte[normalized.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = byte.Parse(normalized.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
        }

        return Encoding.UTF8.GetString(bytes);
    }

    private static string NormalizeText(string text)
    {
        return WhitespaceRegex().Replace(text, " ").Trim();
    }

    [GeneratedRegex(@"/Type\s*/Page(?!s)")]
    private static partial Regex PageRegex();

    [GeneratedRegex(@"stream\r?\n(.*?)\r?\nendstream", RegexOptions.Singleline)]
    private static partial Regex StreamRegex();

    [GeneratedRegex(@"\(((?:\\.|[^\\()])*)\)")]
    private static partial Regex LiteralStringRegex();

    [GeneratedRegex(@"<([0-9A-Fa-f\s]{4,})>")]
    private static partial Regex HexStringRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
