namespace Reckonry.Pricing.Abstractions;

public interface IPriceProvider
{
    string ProviderId { get; }

    Task<PriceQuoteResult> GetQuoteAsync(
        PriceQuoteRequest request,
        CancellationToken cancellationToken = default);
}
