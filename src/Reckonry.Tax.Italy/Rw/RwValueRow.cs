namespace Reckonry.Tax.Italy.Rw;

public sealed record RwValueRow(
    string AssetSymbol,
    decimal OpeningQuantity,
    decimal ClosingQuantity,
    decimal IncomingValueEUR,
    decimal OutgoingValueEUR,
    decimal FeeValueEUR,
    string Warning);
