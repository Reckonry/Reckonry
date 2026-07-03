namespace LedgerForge.Tax.Italy.Rw;

public sealed record ItalyRwReportConfiguration
{
    public RwOwnershipTitle? OwnershipTitle { get; init; }

    public RwPossessionType? PossessionType { get; init; }

    public decimal? OwnershipPercentage { get; init; }

    public IReadOnlyCollection<string> CoOwners { get; init; } = Array.Empty<string>();

    public decimal? PriorCryptoTaxCredit { get; init; }

    public decimal? CryptoTaxF24Compensations { get; init; }

    public decimal? CryptoTaxAdvancesPaid { get; init; }

    public bool? MonitoringOnly { get; init; }

    public string? ForeignStateCode { get; init; }

    public IReadOnlyDictionary<string, decimal> AllowedForeignTaxCreditsByAsset { get; init; }
        = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> CryptoAssetSymbols { get; init; } = Array.Empty<string>();
}
