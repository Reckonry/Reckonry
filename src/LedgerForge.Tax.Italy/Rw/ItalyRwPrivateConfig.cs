namespace LedgerForge.Tax.Italy.Rw;

public sealed record ItalyRwPrivateConfig
{
    public int Year { get; init; }

    public ItalyRwTaxpayerConfiguration TaxpayerConfiguration { get; init; } = new();

    public ItalyRw8InputConfiguration Rw8Inputs { get; init; } = new();

    public string? ReconciliationSummaryPath { get; init; }

    public IReadOnlyList<ItalyRwPrivateAssetConfig> Assets { get; init; } = Array.Empty<ItalyRwPrivateAssetConfig>();

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

public sealed record ItalyRwTaxpayerConfiguration
{
    public string? OwnershipTitle { get; init; }

    public string? PossessionType { get; init; }

    public decimal? OwnershipPercentage { get; init; }

    public bool? MonitoringOnly { get; init; }

    public string? ForeignStateCode { get; init; }

    public string? ForeignStateTreatmentNotes { get; init; }
}

public sealed record ItalyRw8InputConfiguration
{
    public decimal? PriorCryptoTaxCredit { get; init; }

    public decimal? CryptoTaxF24Compensations { get; init; }

    public decimal? CryptoTaxAdvancesPaid { get; init; }
}

public sealed record ItalyRwPrivateAssetConfig
{
    public required string AssetSymbol { get; init; }

    public string? ValuationCriterion { get; init; }

    public ItalyRwPrivateValuationEvidence? InitialValue { get; init; }

    public ItalyRwPrivateValuationEvidence? FinalValue { get; init; }
}

public sealed record ItalyRwPrivateValuationEvidence
{
    public string? Type { get; init; }

    public decimal? ValueEur { get; init; }

    public string? SourceName { get; init; }

    public DateTimeOffset? SourceTimestamp { get; init; }

    public decimal? Confidence { get; init; }

    public string? Notes { get; init; }
}
