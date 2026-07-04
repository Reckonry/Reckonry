using System.Globalization;
using Reckonry.Core;
using Reckonry.Importers.Abstractions;

namespace Reckonry.Importers.Coinbase;

public sealed class CoinbaseImporter : IExchangeImporter
{
    private const string SourceSystem = "Coinbase";

    public ImporterDescriptor Descriptor { get; } = new()
    {
        Id = "coinbase",
        DisplayName = "Coinbase CSV Importer",
        Provider = "Coinbase",
        ImporterVersion = "0.1.0-alpha",
        CoveragePercent = 45m,
        SupportedFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".csv" },
        SupportedFiles =
        [
            "Coinbase transaction CSV export",
            "Normalized Coinbase demo CSV"
        ],
        SupportedSchemas =
        [
            "Timestamp,Transaction Type,Asset,Quantity Transacted,Spot Price Currency,Subtotal,Total (inclusive of fees and/or spread),Fees and/or Spread,Notes",
            "timestamp,type,asset,quantity,native_currency,native_amount,fee_amount,fee_currency,received_asset,received_quantity,notes"
        ],
        SupportedOperations =
        [
            "Buys",
            "Sells",
            "Sends",
            "Receives",
            "Rewards and earn entries",
            "Conversions",
            "Unknown row preservation"
        ]
    };

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
            var rowNumber = 0;
            CoinbaseCsvRecord? header = null;

            foreach (var rawRow in File.ReadLines(csvFile))
            {
                rowNumber++;

                if (string.IsNullOrWhiteSpace(rawRow))
                {
                    continue;
                }

                var record = CoinbaseCsvRecord.Parse(rawRow);
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
        CoinbaseCsvRecord header,
        CoinbaseCsvRecord record)
    {
        var row = CoinbaseCsvRow.From(header, record);

        if (TryParseNormalizedTransaction(csvFile, rowNumber, rawRow, row, out var normalizedEvent))
        {
            return normalizedEvent;
        }

        if (TryParseCoinbaseTransaction(csvFile, rowNumber, rawRow, row, out var coinbaseEvent))
        {
            return coinbaseEvent;
        }

        return CreateUnknownEvent(csvFile, rowNumber, rawRow, row);
    }

    private static bool TryParseCoinbaseTransaction(
        string csvFile,
        int rowNumber,
        string rawRow,
        CoinbaseCsvRow row,
        out LedgerEvent ledgerEvent)
    {
        ledgerEvent = default!;

        if (!row.TryGet("Timestamp", out var timestampText)
            || !row.TryGet("Transaction Type", out var transactionType)
            || !row.TryGet("Asset", out var asset)
            || !row.TryGet("Quantity Transacted", out var quantityText)
            || !TryParseTimestamp(timestampText, out var timestamp)
            || !TryParseDecimal(quantityText, out var quantity))
        {
            return false;
        }

        var normalizedType = NormalizeType(transactionType);
        var nativeCurrency = row.GetOrDefault("Spot Price Currency", row.GetOrDefault("Native Currency", "EUR"));
        var subtotal = TryGetNativeAmount(row, "Subtotal", nativeCurrency);
        var total = TryGetNativeAmount(row, "Total (inclusive of fees and/or spread)", nativeCurrency);
        var fee = TryGetNativeAmount(row, "Fees and/or Spread", nativeCurrency);
        var notes = row.GetOrDefault("Notes", string.Empty);

        ledgerEvent = normalizedType switch
        {
            "buy" => CreateBuyEvent(csvFile, rowNumber, rawRow, timestamp, asset, quantity, nativeCurrency, subtotal, total, fee),
            "sell" => CreateSellEvent(csvFile, rowNumber, rawRow, timestamp, asset, quantity, nativeCurrency, subtotal, fee),
            "send" or "withdrawal" => CreateSinglePostingEvent(csvFile, rowNumber, rawRow, timestamp, LedgerEventType.Withdrawal, $"Coinbase send {asset}", asset, quantity, LedgerPostingDirection.Out, "Coinbase:Sent"),
            "receive" or "deposit" => CreateSinglePostingEvent(csvFile, rowNumber, rawRow, timestamp, ResolveReceiveEventType(notes), BuildReceiveDescription(asset, notes), asset, quantity, LedgerPostingDirection.In, "Coinbase:Received"),
            "reward" or "learningreward" or "stakingreward" => CreateSinglePostingEvent(csvFile, rowNumber, rawRow, timestamp, LedgerEventType.Reward, $"Coinbase reward {asset}", asset, quantity, LedgerPostingDirection.In, "Coinbase:Rewards"),
            _ => null!
        };

        return ledgerEvent is not null;
    }

    private static bool TryParseNormalizedTransaction(
        string csvFile,
        int rowNumber,
        string rawRow,
        CoinbaseCsvRow row,
        out LedgerEvent ledgerEvent)
    {
        ledgerEvent = default!;

        if (!row.TryGet("timestamp", out var timestampText)
            || !row.TryGet("type", out var transactionType)
            || !TryParseTimestamp(timestampText, out var timestamp))
        {
            return false;
        }

        var normalizedType = NormalizeType(transactionType);
        if (normalizedType == "convert")
        {
            return TryParseNormalizedConversion(csvFile, rowNumber, rawRow, row, timestamp, out ledgerEvent);
        }

        if (!row.TryGet("asset", out var asset)
            || !row.TryGet("quantity", out var quantityText)
            || !TryParseDecimal(quantityText, out var quantity))
        {
            return false;
        }

        var nativeCurrency = row.GetOrDefault("native_currency", "EUR");
        var nativeAmount = TryGetNativeAmount(row, "native_amount", nativeCurrency);
        var feeAmount = TryGetNativeAmount(row, "fee_amount", row.GetOrDefault("fee_currency", nativeCurrency));
        var notes = row.GetOrDefault("notes", string.Empty);

        ledgerEvent = normalizedType switch
        {
            "buy" => CreateBuyEvent(csvFile, rowNumber, rawRow, timestamp, asset, quantity, nativeCurrency, nativeAmount, nativeAmount, feeAmount),
            "sell" => CreateSellEvent(csvFile, rowNumber, rawRow, timestamp, asset, quantity, nativeCurrency, nativeAmount, feeAmount),
            "send" or "withdrawal" => CreateSinglePostingEvent(csvFile, rowNumber, rawRow, timestamp, LedgerEventType.Withdrawal, $"Coinbase send {asset}", asset, quantity, LedgerPostingDirection.Out, "Coinbase:Sent"),
            "receive" or "deposit" => CreateSinglePostingEvent(csvFile, rowNumber, rawRow, timestamp, ResolveReceiveEventType(notes), BuildReceiveDescription(asset, notes), asset, quantity, LedgerPostingDirection.In, "Coinbase:Received"),
            "reward" or "learningreward" or "stakingreward" => CreateSinglePostingEvent(csvFile, rowNumber, rawRow, timestamp, LedgerEventType.Reward, $"Coinbase reward {asset}", asset, quantity, LedgerPostingDirection.In, "Coinbase:Rewards"),
            _ => null!
        };

        return ledgerEvent is not null;
    }

    private static bool TryParseNormalizedConversion(
        string csvFile,
        int rowNumber,
        string rawRow,
        CoinbaseCsvRow row,
        DateTimeOffset timestamp,
        out LedgerEvent ledgerEvent)
    {
        ledgerEvent = default!;

        if (!row.TryGet("asset", out var sentAsset)
            || !row.TryGet("quantity", out var sentQuantityText)
            || !row.TryGet("received_asset", out var receivedAsset)
            || !row.TryGet("received_quantity", out var receivedQuantityText)
            || !TryParseDecimal(sentQuantityText, out var sentQuantity)
            || !TryParseDecimal(receivedQuantityText, out var receivedQuantity))
        {
            return false;
        }

        var nativeCurrency = row.GetOrDefault("native_currency", "EUR");
        var nativeAmount = TryGetNativeAmount(row, "native_amount", nativeCurrency);
        var fee = TryGetNativeAmount(row, "fee_amount", row.GetOrDefault("fee_currency", nativeCurrency));
        var postings = new List<LedgerPosting>
        {
            new(sentAsset, Math.Abs(sentQuantity), LedgerPostingDirection.Out, "Coinbase:Convert", nativeAmount),
            new(receivedAsset, Math.Abs(receivedQuantity), LedgerPostingDirection.In, "Coinbase:Convert", nativeAmount)
        };

        if (fee is not null && fee.Amount > 0)
        {
            postings.Add(new LedgerPosting(fee.CurrencyCode, fee.Amount, LedgerPostingDirection.Out, "Coinbase:Fees", fee));
        }

        ledgerEvent = new LedgerEvent(
            Guid.NewGuid(),
            timestamp,
            LedgerEventType.Conversion,
            $"Coinbase conversion {sentAsset} to {receivedAsset}",
            CreateSourceReference(csvFile, rowNumber, rawRow),
            postings);

        return true;
    }

    private static LedgerEvent CreateBuyEvent(
        string csvFile,
        int rowNumber,
        string rawRow,
        DateTimeOffset timestamp,
        string asset,
        decimal quantity,
        string nativeCurrency,
        MoneyAmount? subtotal,
        MoneyAmount? total,
        MoneyAmount? fee)
    {
        var fiatOut = subtotal ?? total;
        var postings = new List<LedgerPosting>
        {
            new(asset, Math.Abs(quantity), LedgerPostingDirection.In, "Coinbase:Buy", subtotal)
        };

        if (fiatOut is not null && fiatOut.Amount > 0)
        {
            postings.Add(new LedgerPosting(nativeCurrency, fiatOut.Amount, LedgerPostingDirection.Out, "Coinbase:Fiat", fiatOut));
        }

        if (fee is not null && fee.Amount > 0)
        {
            postings.Add(new LedgerPosting(fee.CurrencyCode, fee.Amount, LedgerPostingDirection.Out, "Coinbase:Fees", fee));
        }

        return new LedgerEvent(
            Guid.NewGuid(),
            timestamp,
            LedgerEventType.Trade,
            $"Coinbase buy {asset}",
            CreateSourceReference(csvFile, rowNumber, rawRow),
            postings);
    }

    private static LedgerEvent CreateSellEvent(
        string csvFile,
        int rowNumber,
        string rawRow,
        DateTimeOffset timestamp,
        string asset,
        decimal quantity,
        string nativeCurrency,
        MoneyAmount? subtotal,
        MoneyAmount? fee)
    {
        var postings = new List<LedgerPosting>
        {
            new(asset, Math.Abs(quantity), LedgerPostingDirection.Out, "Coinbase:Sell", subtotal)
        };

        if (subtotal is not null && subtotal.Amount > 0)
        {
            postings.Add(new LedgerPosting(nativeCurrency, subtotal.Amount, LedgerPostingDirection.In, "Coinbase:Fiat", subtotal));
        }

        if (fee is not null && fee.Amount > 0)
        {
            postings.Add(new LedgerPosting(fee.CurrencyCode, fee.Amount, LedgerPostingDirection.Out, "Coinbase:Fees", fee));
        }

        return new LedgerEvent(
            Guid.NewGuid(),
            timestamp,
            LedgerEventType.Trade,
            $"Coinbase sell {asset}",
            CreateSourceReference(csvFile, rowNumber, rawRow),
            postings);
    }

    private static LedgerEvent CreateSinglePostingEvent(
        string csvFile,
        int rowNumber,
        string rawRow,
        DateTimeOffset timestamp,
        LedgerEventType eventType,
        string description,
        string asset,
        decimal quantity,
        LedgerPostingDirection direction,
        string account)
    {
        return new LedgerEvent(
            Guid.NewGuid(),
            timestamp,
            eventType,
            description,
            CreateSourceReference(csvFile, rowNumber, rawRow),
            [new LedgerPosting(asset, Math.Abs(quantity), direction, account)]);
    }

    private static LedgerEvent CreateUnknownEvent(string csvFile, int rowNumber, string rawRow, CoinbaseCsvRow row)
    {
        var timestamp = TryGetFallbackTimestamp(row, out var parsedTimestamp)
            ? parsedTimestamp
            : DateTimeOffset.UnixEpoch;

        return new LedgerEvent(
            Guid.NewGuid(),
            timestamp,
            LedgerEventType.Unknown,
            "Unsupported Coinbase CSV row preserved for later classification.",
            CreateSourceReference(csvFile, rowNumber, rawRow),
            Array.Empty<LedgerPosting>());
    }

    private static SourceReference CreateSourceReference(string csvFile, int rowNumber, string rawRow)
    {
        return new SourceReference(SourceSystem, Path.GetFileName(csvFile), rowNumber, rawRow);
    }

    private static LedgerEventType ResolveReceiveEventType(string notes)
    {
        return IsRewardText(notes) ? LedgerEventType.Reward : LedgerEventType.Deposit;
    }

    private static string BuildReceiveDescription(string asset, string notes)
    {
        return IsRewardText(notes) ? $"Coinbase reward {asset}" : $"Coinbase receive {asset}";
    }

    private static bool IsRewardText(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        return normalized.Contains("reward", StringComparison.Ordinal)
            || normalized.Contains("earn", StringComparison.Ordinal)
            || normalized.Contains("staking", StringComparison.Ordinal)
            || normalized.Contains("learning", StringComparison.Ordinal);
    }

    private static MoneyAmount? TryGetNativeAmount(CoinbaseCsvRow row, string amountColumn, string currencyCode)
    {
        if (!row.TryGet(amountColumn, out var amountText)
            || !TryParseDecimal(amountText, out var amount)
            || amount == 0)
        {
            return null;
        }

        return new MoneyAmount(currencyCode.Trim().ToUpperInvariant(), Math.Abs(amount));
    }

    private static string NormalizeType(string value)
    {
        return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }

    private static bool TryGetFallbackTimestamp(CoinbaseCsvRow row, out DateTimeOffset timestamp)
    {
        foreach (var column in new[] { "Timestamp", "Time", "Date", "timestamp", "Mystery Time" })
        {
            if (!row.TryGet(column, out var value) || string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (TryParseTimestamp(value, out timestamp))
            {
                return true;
            }
        }

        timestamp = default;
        return false;
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
}
