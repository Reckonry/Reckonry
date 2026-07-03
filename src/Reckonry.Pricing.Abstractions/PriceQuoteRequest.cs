namespace Reckonry.Pricing.Abstractions;

public sealed record PriceQuoteRequest(
    string AssetSymbol,
    string CurrencyCode,
    DateTimeOffset TimestampUtc);
