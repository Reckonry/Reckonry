using System.Text;

namespace Reckonry.Importers.Binance;

internal sealed class BinanceCsvRow
{
    private readonly Dictionary<string, string> _values;

    private BinanceCsvRow(Dictionary<string, string> values)
    {
        _values = values;
    }

    public static BinanceCsvRow From(CsvRecord header, CsvRecord record)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < header.Values.Count; i++)
        {
            var headerName = header.Values[i];
            if (string.IsNullOrWhiteSpace(headerName))
            {
                continue;
            }

            values[Normalize(headerName)] = i < record.Values.Count ? record.Values[i] : string.Empty;
        }

        return new BinanceCsvRow(values);
    }

    public bool TryGet(string name, out string value)
    {
        return _values.TryGetValue(Normalize(name), out value!);
    }

    public string GetOrDefault(string name, string fallback)
    {
        return TryGet(name, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }

    private static string Normalize(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var currentChar in value)
        {
            if (char.IsLetterOrDigit(currentChar))
            {
                builder.Append(char.ToLowerInvariant(currentChar));
            }
        }

        return builder.ToString();
    }
}
