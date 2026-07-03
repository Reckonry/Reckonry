namespace Reckonry.Tax.Italy.Rw;

internal static class ItalyRwAssetClassifier
{
    private static readonly HashSet<string> FiatAssetSymbols = new(StringComparer.OrdinalIgnoreCase)
    {
        "AUD",
        "CAD",
        "CHF",
        "EUR",
        "GBP",
        "JPY",
        "USD"
    };

    public static bool IsCandidateCryptoAsset(string assetSymbol)
    {
        return !string.IsNullOrWhiteSpace(assetSymbol)
            && !FiatAssetSymbols.Contains(assetSymbol.Trim());
    }
}
