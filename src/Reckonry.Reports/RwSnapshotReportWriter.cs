using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Reckonry.Core;

namespace Reckonry.Reports;

public sealed class RwSnapshotReportWriter : IRwSnapshotReportWriter
{
    private const string UnknownAssetSymbol = "UNKNOWN";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<IReadOnlyList<RwSnapshotRow>> WriteAsync(
        string outputFolder,
        int year,
        IReadOnlyCollection<LedgerEvent> events,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFolder);
        ArgumentNullException.ThrowIfNull(events);

        var rows = BuildRows(year, events);
        Directory.CreateDirectory(outputFolder);

        var csvPath = Path.Combine(outputFolder, $"rw-snapshot-{year}.csv");
        var jsonPath = Path.Combine(outputFolder, $"rw-snapshot-{year}.json");

        await File.WriteAllTextAsync(csvPath, BuildCsv(rows), cancellationToken);
        await using (var jsonStream = File.Create(jsonPath))
        {
            await JsonSerializer.SerializeAsync(jsonStream, rows, JsonOptions, cancellationToken);
        }

        return rows;
    }

    public static IReadOnlyList<RwSnapshotRow> BuildRows(int year, IReadOnlyCollection<LedgerEvent> events)
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

                if (ledgerEvent.TimestampUtc >= start && ledgerEvent.TimestampUtc < endExclusive)
                {
                    if (posting.Direction == LedgerPostingDirection.In)
                    {
                        accumulator.IncomingQuantity += posting.Amount;
                    }
                    else
                    {
                        accumulator.OutgoingQuantity += posting.Amount;
                    }

                    accumulator.ClosingQuantity += signedAmount;
                }
            }
        }

        return accumulatorByAsset.Values
            .Select(ToRow)
            .Where(ShouldInclude)
            .OrderBy(r => r.AssetSymbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();
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

    private static RwSnapshotRow ToRow(AssetAccumulator accumulator)
    {
        var warning = accumulator.UnknownEventCount == 0
            ? string.Empty
            : accumulator.AssetSymbol == UnknownAssetSymbol
                ? "Unknown events without asset postings may affect balances."
                : "Unknown events for this asset may affect balances.";

        return new RwSnapshotRow(
            accumulator.AssetSymbol,
            accumulator.OpeningQuantity,
            accumulator.ClosingQuantity,
            accumulator.IncomingQuantity,
            accumulator.OutgoingQuantity,
            accumulator.UnknownEventCount,
            warning);
    }

    private static bool ShouldInclude(RwSnapshotRow row)
    {
        return row.OpeningQuantity != 0
            || row.ClosingQuantity != 0
            || row.IncomingQuantity != 0
            || row.OutgoingQuantity != 0
            || row.UnknownEventCount != 0;
    }

    private static string BuildCsv(IEnumerable<RwSnapshotRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("AssetSymbol,OpeningQuantity,ClosingQuantity,IncomingQuantity,OutgoingQuantity,UnknownEventCount,Warning");

        foreach (var row in rows)
        {
            builder
                .Append(EscapeCsv(row.AssetSymbol))
                .Append(',')
                .Append(FormatDecimal(row.OpeningQuantity))
                .Append(',')
                .Append(FormatDecimal(row.ClosingQuantity))
                .Append(',')
                .Append(FormatDecimal(row.IncomingQuantity))
                .Append(',')
                .Append(FormatDecimal(row.OutgoingQuantity))
                .Append(',')
                .Append(row.UnknownEventCount.ToString(CultureInfo.InvariantCulture))
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

        public decimal IncomingQuantity { get; set; }

        public decimal OutgoingQuantity { get; set; }

        public int UnknownEventCount { get; set; }
    }
}
