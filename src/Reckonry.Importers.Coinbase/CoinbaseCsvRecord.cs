using System.Text;

namespace Reckonry.Importers.Coinbase;

internal sealed class CoinbaseCsvRecord
{
    private CoinbaseCsvRecord(IReadOnlyList<string> values)
    {
        Values = values;
    }

    public IReadOnlyList<string> Values { get; }

    public static CoinbaseCsvRecord Parse(string rawRow)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < rawRow.Length; i++)
        {
            var currentChar = rawRow[i];

            if (currentChar == '"')
            {
                if (inQuotes && i + 1 < rawRow.Length && rawRow[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (currentChar == ',' && !inQuotes)
            {
                values.Add(current.ToString().Trim());
                current.Clear();
                continue;
            }

            current.Append(currentChar);
        }

        values.Add(current.ToString().Trim());
        return new CoinbaseCsvRecord(values);
    }
}
