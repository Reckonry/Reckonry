namespace Reckonry.Reports;

public sealed record RwSnapshotRow(
    string AssetSymbol,
    decimal OpeningQuantity,
    decimal ClosingQuantity,
    decimal IncomingQuantity,
    decimal OutgoingQuantity,
    int UnknownEventCount,
    string Warning);
