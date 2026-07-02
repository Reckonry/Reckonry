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

        foreach (var csvFile in Directory.EnumerateFiles(inputFolder, "*.csv", SearchOption.AllDirectories).Order())
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

        if (TryParseNormalizedTransaction(csvFile, rowNumber, rawRow, row, out var normalizedEvent))
        {
            return normalizedEvent;
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

    private static bool TryParseNormalizedTransaction(
        string csvFile,
        int rowNumber,
        string rawRow,
        BinanceCsvRow row,
        out LedgerEvent ledgerEvent)
    {
        ledgerEvent = default!;

        if (!row.TryGet("datetime_tz_CET", out var timestampText)
            || !row.TryGet("type", out var transactionType)
            || !TryParseCetTimestamp(timestampText, out var timestamp))
        {
            return false;
        }

        var normalizedType = transactionType.Trim().ToLowerInvariant();
        var postings = new List<LedgerPosting>();

        AddOptionalPosting(row, "sent_amount", "sent_currency", "sent_value_EUR", LedgerPostingDirection.Out, "Binance:Sent", postings);
        AddOptionalPosting(row, "received_amount", "received_currency", "received_value_EUR", LedgerPostingDirection.In, "Binance:Received", postings);
        AddOptionalPosting(row, "fee_amount", "fee_currency", "fee_value_EUR", LedgerPostingDirection.Out, "Binance:Fees", postings);

        if (postings.Count == 0)
        {
            return false;
        }

        var eventType = normalizedType switch
        {
            "receive" or "deposit" => ResolveReceiveEventType(row),
            "send" => LedgerEventType.Withdrawal,
            "trade" or "buy" or "sell" => LedgerEventType.Trade,
            _ => LedgerEventType.Unknown
        };

        if (eventType == LedgerEventType.Unknown)
        {
            return false;
        }

        ledgerEvent = new LedgerEvent(
            Guid.NewGuid(),
            timestamp,
            eventType,
            $"Binance normalized {normalizedType}",
            CreateSourceReference(csvFile, rowNumber, rawRow),
            postings);

        return true;
    }

    private static LedgerEventType ResolveReceiveEventType(BinanceCsvRow row)
    {
        var label = row.GetOrDefault("label", string.Empty).Trim().ToLowerInvariant();
        return IsRewardOperation(label) ? LedgerEventType.Reward : LedgerEventType.Deposit;
    }

    private static void AddOptionalPosting(
        BinanceCsvRow row,
        string amountColumn,
        string assetColumn,
        string valueEurColumn,
        LedgerPostingDirection direction,
        string account,
        ICollection<LedgerPosting> postings)
    {
        if (!row.TryGet(amountColumn, out var amountText)
            || !row.TryGet(assetColumn, out var assetSymbol)
            || string.IsNullOrWhiteSpace(assetSymbol)
            || !TryParseDecimal(amountText, out var amount)
            || amount == 0)
        {
            return;
        }

        var value = TryGetEurValue(row, valueEurColumn);
        postings.Add(new LedgerPosting(assetSymbol.Trim(), Math.Abs(amount), direction, account, value));
    }

    private static MoneyAmount? TryGetEurValue(BinanceCsvRow row, string valueEurColumn)
    {
        if (!row.TryGet(valueEurColumn, out var valueText)
            || !TryParseDecimal(valueText, out var value)
            || value == 0)
        {
            return null;
        }

        return new MoneyAmount("EUR", Math.Abs(value));
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

    private static bool TryParseCetTimestamp(string value, out DateTimeOffset timestamp)
    {
        if (DateTime.TryParseExact(
            value,
            "yyyy-MM-dd-HH:mm:ss",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var localDateTime))
        {
            timestamp = new DateTimeOffset(localDateTime, TimeSpan.FromHours(1)).ToUniversalTime();
            return true;
        }

        return TryParseTimestamp(value, out timestamp);
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
