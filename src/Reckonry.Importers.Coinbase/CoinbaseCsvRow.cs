using System.Text;

namespace Reckonry.Importers.Coinbase;

internal sealed class CoinbaseCsvRow
{
    private readonly Dictionary<string, string> values;

    private CoinbaseCsvRow(Dictionary<string, string> values)
    {
        this.values = values;
    }

    public static CoinbaseCsvRow From(CoinbaseCsvRecord header, CoinbaseCsvRecord record)
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

        return new CoinbaseCsvRow(values);
    }

    public bool TryGet(string name, out string value)
    {
        return values.TryGetValue(Normalize(name), out value!);
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
