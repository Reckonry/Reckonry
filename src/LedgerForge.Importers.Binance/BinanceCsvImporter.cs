using LedgerForge.Core;
using System.Globalization;

namespace LedgerForge.Importers.Binance;

public sealed class BinanceCsvImporter : IBinanceCsvImporter
{
    private const string SourceSystem = "Binance";

    public IReadOnlyList<LedgerEvent> ImportFolder(string inputFolder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputFolder);

        if (!Directory.Exists(inputFolder))
        {
            throw new DirectoryNotFoundException($"Input folder was not found: {inputFolder}");
        }

        var events = new List<LedgerEvent>();

        foreach (var csvFile in Directory.EnumerateFiles(inputFolder, "*.csv", SearchOption.TopDirectoryOnly).Order())
        {
            // TODO: Expand coverage for Binance Pay, Futures, Margin, Convert history variants, and schema changes.
            var rowNumber = 0;
            CsvRecord? header = null;

            foreach (var rawRow in File.ReadLines(csvFile))
            {
                rowNumber++;

                if (string.IsNullOrWhiteSpace(rawRow))
                {
                    continue;
                }

                var record = CsvRecord.Parse(rawRow);
                if (header is null)
                {
                    header = record;
                    continue;
                }

                events.Add(ParseDataRow(csvFile, rowNumber, rawRow, header, record));
            }
        }

        return events;
    }

    private static LedgerEvent ParseDataRow(
        string csvFile,
        int rowNumber,
        string rawRow,
        CsvRecord header,
        CsvRecord record)
    {
        var row = BinanceCsvRow.From(header, record);

        if (TryParseUniversalTransaction(csvFile, rowNumber, rawRow, row, out var universalEvent))
        {
            return universalEvent;
        }

        if (TryParseSpotTrade(csvFile, rowNumber, rawRow, row, out var tradeEvent))
        {
            return tradeEvent;
        }

        if (TryParseConversion(csvFile, rowNumber, rawRow, row, out var conversionEvent))
        {
            return conversionEvent;
        }

        return CreateUnknownEvent(csvFile, rowNumber, rawRow);
    }

    private static bool TryParseUniversalTransaction(
        string csvFile,
        int rowNumber,
        string rawRow,
        BinanceCsvRow row,
        out LedgerEvent ledgerEvent)
    {
        ledgerEvent = default!;

        if (!row.TryGet("UTC_Time", out var timestampText)
            && !row.TryGet("UTC Time", out timestampText)
            && !row.TryGet("Date", out timestampText))
        {
            return false;
        }

        if (!row.TryGet("Operation", out var operation)
            || !row.TryGet("Coin", out var asset)
            || !row.TryGet("Change", out var amountText)
            || !TryParseTimestamp(timestampText, out var timestamp)
            || !TryParseDecimal(amountText, out var amount))
        {
            return false;
        }

        var account = row.GetOrDefault("Account", "Binance");
        var remark = row.GetOrDefault("Remark", string.Empty);
        var normalizedOperation = operation.Trim().ToLowerInvariant();

        if (normalizedOperation.Contains("deposit", StringComparison.Ordinal))
        {
            ledgerEvent = CreateSinglePostingEvent(
                csvFile,
                rowNumber,
                rawRow,
                timestamp,
                LedgerEventType.Deposit,
                $"Binance deposit {asset}",
                asset,
                Math.Abs(amount),
                LedgerPostingDirection.In,
                $"Binance:{account}");
            return true;
        }

        if (normalizedOperation.Contains("withdraw", StringComparison.Ordinal))
        {
            ledgerEvent = CreateSinglePostingEvent(
                csvFile,
                rowNumber,
                rawRow,
                timestamp,
                LedgerEventType.Withdrawal,
                $"Binance withdrawal {asset}",
                asset,
                Math.Abs(amount),
                LedgerPostingDirection.Out,
                $"Binance:{account}");
            return true;
        }

        if (IsRewardOperation(normalizedOperation))
        {
            ledgerEvent = CreateSinglePostingEvent(
                csvFile,
                rowNumber,
                rawRow,
                timestamp,
                LedgerEventType.Reward,
                string.IsNullOrWhiteSpace(remark) ? $"Binance reward {asset}" : remark,
                asset,
                Math.Abs(amount),
                LedgerPostingDirection.In,
                $"Binance:{account}");
            return true;
        }

        return false;
    }

    private static bool TryParseSpotTrade(
        string csvFile,
        int rowNumber,
        string rawRow,
        BinanceCsvRow row,
        out LedgerEvent ledgerEvent)
    {
        ledgerEvent = default!;

        if (!row.TryGet("Date(UTC)", out var timestampText)
            && !row.TryGet("Date", out timestampText)
            && !row.TryGet("UTC_Time", out timestampText))
        {
            return false;
        }

        if (!row.TryGet("Market", out var market)
            || !row.TryGet("Type", out var side)
            || !row.TryGet("Amount", out var baseAmountText)
            || !row.TryGet("Total", out var quoteAmountText)
            || !TryParseTimestamp(timestampText, out var timestamp)
            || !TryParseMarket(market, out var baseAsset, out var quoteAsset)
            || !TryParseDecimal(baseAmountText, out var baseAmount)
            || !TryParseDecimal(quoteAmountText, out var quoteAmount))
        {
            return false;
        }

        var normalizedSide = side.Trim().ToLowerInvariant();
        if (normalizedSide is not ("buy" or "sell"))
        {
            return false;
        }

        var postings = new List<LedgerPosting>();

        if (normalizedSide == "buy")
        {
            postings.Add(new LedgerPosting(baseAsset, Math.Abs(baseAmount), LedgerPostingDirection.In, "Binance:Spot"));
            postings.Add(new LedgerPosting(quoteAsset, Math.Abs(quoteAmount), LedgerPostingDirection.Out, "Binance:Spot"));
        }
        else
        {
            postings.Add(new LedgerPosting(baseAsset, Math.Abs(baseAmount), LedgerPostingDirection.Out, "Binance:Spot"));
            postings.Add(new LedgerPosting(quoteAsset, Math.Abs(quoteAmount), LedgerPostingDirection.In, "Binance:Spot"));
        }

        if (row.TryGet("Fee", out var feeText)
            && row.TryGet("Fee Coin", out var feeAsset)
            && TryParseDecimal(feeText, out var feeAmount)
            && feeAmount != 0)
        {
            postings.Add(new LedgerPosting(feeAsset, Math.Abs(feeAmount), LedgerPostingDirection.Out, "Binance:Fees"));
        }

        ledgerEvent = new LedgerEvent(
            Guid.NewGuid(),
            timestamp,
            LedgerEventType.Trade,
            $"Binance spot {normalizedSide} {baseAsset}/{quoteAsset}",
            CreateSourceReference(csvFile, rowNumber, rawRow),
            postings);

        return true;
    }

    private static bool TryParseConversion(
        string csvFile,
        int rowNumber,
        string rawRow,
        BinanceCsvRow row,
        out LedgerEvent ledgerEvent)
    {
        ledgerEvent = default!;

        if (!row.TryGet("Date", out var timestampText)
            && !row.TryGet("Date(UTC)", out timestampText)
            && !row.TryGet("UTC_Time", out timestampText))
        {
            return false;
        }

        if (!row.TryGet("From Asset", out var fromAsset)
            || !row.TryGet("From Amount", out var fromAmountText)
            || !row.TryGet("To Asset", out var toAsset)
            || !row.TryGet("To Amount", out var toAmountText)
            || !TryParseTimestamp(timestampText, out var timestamp)
            || !TryParseDecimal(fromAmountText, out var fromAmount)
            || !TryParseDecimal(toAmountText, out var toAmount))
        {
            return false;
        }

        var postings = new List<LedgerPosting>
        {
            new(fromAsset, Math.Abs(fromAmount), LedgerPostingDirection.Out, "Binance:Convert"),
            new(toAsset, Math.Abs(toAmount), LedgerPostingDirection.In, "Binance:Convert")
        };

        if (row.TryGet("Fee", out var feeText)
            && row.TryGet("Fee Coin", out var feeAsset)
            && TryParseDecimal(feeText, out var feeAmount)
            && feeAmount != 0)
        {
            postings.Add(new LedgerPosting(feeAsset, Math.Abs(feeAmount), LedgerPostingDirection.Out, "Binance:Fees"));
        }

        ledgerEvent = new LedgerEvent(
            Guid.NewGuid(),
            timestamp,
            LedgerEventType.Conversion,
            $"Binance conversion {fromAsset} to {toAsset}",
            CreateSourceReference(csvFile, rowNumber, rawRow),
            postings);

        return true;
    }

    private static LedgerEvent CreateSinglePostingEvent(
        string csvFile,
        int rowNumber,
        string rawRow,
        DateTimeOffset timestamp,
        LedgerEventType eventType,
        string description,
        string asset,
        decimal amount,
        LedgerPostingDirection direction,
        string account)
    {
        return new LedgerEvent(
            Guid.NewGuid(),
            timestamp,
            eventType,
            description,
            CreateSourceReference(csvFile, rowNumber, rawRow),
            new[]
            {
                new LedgerPosting(asset, amount, direction, account)
            });
    }

    private static LedgerEvent CreateUnknownEvent(string csvFile, int rowNumber, string rawRow)
    {
        return new LedgerEvent(
            Guid.NewGuid(),
            DateTimeOffset.UnixEpoch,
            LedgerEventType.Unknown,
            "Unsupported Binance CSV row preserved for later classification.",
            CreateSourceReference(csvFile, rowNumber, rawRow),
            Array.Empty<LedgerPosting>());
    }

    private static SourceReference CreateSourceReference(string csvFile, int rowNumber, string rawRow)
    {
        return new SourceReference(SourceSystem, Path.GetFileName(csvFile), rowNumber, rawRow);
    }

    private static bool IsRewardOperation(string normalizedOperation)
    {
        return normalizedOperation.Contains("reward", StringComparison.Ordinal)
            || normalizedOperation.Contains("interest", StringComparison.Ordinal)
            || normalizedOperation.Contains("earn", StringComparison.Ordinal)
            || normalizedOperation.Contains("staking", StringComparison.Ordinal);
    }

    private static bool TryParseTimestamp(string value, out DateTimeOffset timestamp)
    {
        return DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out timestamp);
    }

    private static bool TryParseDecimal(string value, out decimal amount)
    {
        return decimal.TryParse(
            value,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands,
            CultureInfo.InvariantCulture,
            out amount);
    }

    private static bool TryParseMarket(string market, out string baseAsset, out string quoteAsset)
    {
        var normalizedMarket = market.Trim().Replace("/", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
        var quoteAssets = new[] { "USDT", "USDC", "BUSD", "FDUSD", "TUSD", "BTC", "ETH", "BNB", "EUR", "USD", "GBP" };

        foreach (var candidateQuoteAsset in quoteAssets.OrderByDescending(q => q.Length))
        {
            if (!normalizedMarket.EndsWith(candidateQuoteAsset, StringComparison.Ordinal))
            {
                continue;
            }

            var candidateBaseAsset = normalizedMarket[..^candidateQuoteAsset.Length];
            if (string.IsNullOrWhiteSpace(candidateBaseAsset))
            {
                continue;
            }

            baseAsset = candidateBaseAsset;
            quoteAsset = candidateQuoteAsset;
            return true;
        }

        baseAsset = string.Empty;
        quoteAsset = string.Empty;
        return false;
    }
}
