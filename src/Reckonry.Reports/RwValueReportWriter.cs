using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Reckonry.Core;

namespace Reckonry.Reports;

public sealed class RwValueReportWriter : IRwValueReportWriter
{
    private const string UnknownAssetSymbol = "UNKNOWN";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<IReadOnlyList<RwValueRow>> WriteAsync(
        string outputFolder,
        int year,
        IReadOnlyCollection<LedgerEvent> events,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFolder);
        ArgumentNullException.ThrowIfNull(events);

        var rows = BuildRows(year, events);
        Directory.CreateDirectory(outputFolder);

        var csvPath = Path.Combine(outputFolder, $"rw-value-{year}.csv");
        var jsonPath = Path.Combine(outputFolder, $"rw-value-{year}.json");

        await File.WriteAllTextAsync(csvPath, BuildCsv(rows), cancellationToken);
        await using (var jsonStream = File.Create(jsonPath))
        {
            await JsonSerializer.SerializeAsync(jsonStream, rows, JsonOptions, cancellationToken);
        }

        return rows;
    }

    public static IReadOnlyList<RwValueRow> BuildRows(int year, IReadOnlyCollection<LedgerEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        var start = new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endExclusive = start.AddYears(1);
        var accumulatorByAsset = new Dictionary<string, AssetAccumulator>(StringComparer.OrdinalIgnoreCase);

        foreach (var ledgerEvent in events.OrderBy(e => e.TimestampUtc))
        {
            if (ledgerEvent.EventType == LedgerEventType.Unknown)
            {
                AddUnknownEvent(accumulatorByAsset, ledgerEvent);
                continue;
            }

            foreach (var posting in ledgerEvent.Postings)
            {
                if (string.IsNullOrWhiteSpace(posting.AssetSymbol))
                {
                    continue;
                }

                var accumulator = GetAccumulator(accumulatorByAsset, posting.AssetSymbol);
                var signedAmount = posting.Direction == LedgerPostingDirection.In ? posting.Amount : -posting.Amount;

                if (ledgerEvent.TimestampUtc < start)
                {
                    accumulator.OpeningQuantity += signedAmount;
                    accumulator.ClosingQuantity += signedAmount;
                    continue;
                }

                if (ledgerEvent.TimestampUtc < endExclusive)
                {
                    accumulator.ClosingQuantity += signedAmount;
                    AddValue(accumulator, posting);
                }
            }
        }

        return accumulatorByAsset.Values
            .Select(ToRow)
            .Where(ShouldInclude)
            .OrderBy(r => r.AssetSymbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void AddValue(AssetAccumulator accumulator, LedgerPosting posting)
    {
        if (posting.Value is null || !string.Equals(posting.Value.CurrencyCode, "EUR", StringComparison.OrdinalIgnoreCase))
        {
            accumulator.MissingValueCount++;
            return;
        }

        if (string.Equals(posting.Account, "Binance:Fees", StringComparison.OrdinalIgnoreCase))
        {
            accumulator.FeeValueEUR += posting.Value.Amount;
        }
        else if (posting.Direction == LedgerPostingDirection.In)
        {
            accumulator.IncomingValueEUR += posting.Value.Amount;
        }
        else
        {
            accumulator.OutgoingValueEUR += posting.Value.Amount;
        }
    }

    private static void AddUnknownEvent(
        Dictionary<string, AssetAccumulator> accumulatorByAsset,
        LedgerEvent ledgerEvent)
    {
        var assetSymbols = ledgerEvent.Postings
            .Select(p => p.AssetSymbol)
            .Where(asset => !string.IsNullOrWhiteSpace(asset))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (assetSymbols.Length == 0)
        {
            GetAccumulator(accumulatorByAsset, UnknownAssetSymbol).UnknownEventCount++;
            return;
        }

        foreach (var assetSymbol in assetSymbols)
        {
            GetAccumulator(accumulatorByAsset, assetSymbol).UnknownEventCount++;
        }
    }

    private static AssetAccumulator GetAccumulator(
        Dictionary<string, AssetAccumulator> accumulatorByAsset,
        string assetSymbol)
    {
        var normalizedAssetSymbol = assetSymbol.Trim().ToUpperInvariant();
        if (!accumulatorByAsset.TryGetValue(normalizedAssetSymbol, out var accumulator))
        {
            accumulator = new AssetAccumulator(normalizedAssetSymbol);
            accumulatorByAsset[normalizedAssetSymbol] = accumulator;
        }

        return accumulator;
    }

    private static RwValueRow ToRow(AssetAccumulator accumulator)
    {
        var warnings = new List<string>();
        if (accumulator.UnknownEventCount > 0)
        {
            warnings.Add(accumulator.AssetSymbol == UnknownAssetSymbol
                ? "Unknown events without asset postings may affect values."
                : "Unknown events for this asset may affect values.");
        }

        if (accumulator.MissingValueCount > 0)
        {
            warnings.Add("Some postings do not include EUR values.");
        }

        return new RwValueRow(
            accumulator.AssetSymbol,
            accumulator.OpeningQuantity,
            accumulator.ClosingQuantity,
            accumulator.IncomingValueEUR,
            accumulator.OutgoingValueEUR,
            accumulator.FeeValueEUR,
            string.Join(" ", warnings));
    }

    private static bool ShouldInclude(RwValueRow row)
    {
        return row.OpeningQuantity != 0
            || row.ClosingQuantity != 0
            || row.IncomingValueEUR != 0
            || row.OutgoingValueEUR != 0
            || row.FeeValueEUR != 0
            || !string.IsNullOrWhiteSpace(row.Warning);
    }

    private static string BuildCsv(IEnumerable<RwValueRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("AssetSymbol,OpeningQuantity,ClosingQuantity,IncomingValueEUR,OutgoingValueEUR,FeeValueEUR,Warning");

        foreach (var row in rows)
        {
            builder
                .Append(EscapeCsv(row.AssetSymbol))
                .Append(',')
                .Append(FormatDecimal(row.OpeningQuantity))
                .Append(',')
                .Append(FormatDecimal(row.ClosingQuantity))
                .Append(',')
                .Append(FormatDecimal(row.IncomingValueEUR))
                .Append(',')
                .Append(FormatDecimal(row.OutgoingValueEUR))
                .Append(',')
                .Append(FormatDecimal(row.FeeValueEUR))
                .Append(',')
                .Append(EscapeCsv(row.Warning))
                .AppendLine();
        }

        return builder.ToString();
    }

    private static string FormatDecimal(decimal value)
    {
        return value.ToString("0.############################", CultureInfo.InvariantCulture);
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private sealed class AssetAccumulator(string assetSymbol)
    {
        public string AssetSymbol { get; } = assetSymbol;

        public decimal OpeningQuantity { get; set; }

        public decimal ClosingQuantity { get; set; }

        public decimal IncomingValueEUR { get; set; }

        public decimal OutgoingValueEUR { get; set; }

        public decimal FeeValueEUR { get; set; }

        public int UnknownEventCount { get; set; }

        public int MissingValueCount { get; set; }
    }
}
