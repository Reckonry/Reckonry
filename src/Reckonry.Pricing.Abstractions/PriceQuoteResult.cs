namespace Reckonry.Pricing.Abstractions;

public sealed record PriceQuoteResult(
    PriceQuoteRequest Request,
    decimal? Price,
    string Source,
    string? Warning);
