namespace Reckonry.Tax.Italy.Rw;

public sealed record ItalyRwLine
{
    public required string AssetSymbol { get; init; }

    public IReadOnlyCollection<Guid> SourceLedgerEventIds { get; init; } = Array.Empty<Guid>();

    public RwValuationEvidence? InitialValueEvidence { get; init; }

    public RwValuationEvidence? FinalValueEvidence { get; init; }

    public RwOwnershipTitle? Column1OwnershipTitle { get; init; }

    public RwPossessionType? Column2PossessionType { get; init; }

    public int Column3AssetCode { get; init; }

    public string? Column4ForeignStateCode { get; init; }

    public decimal? Column5OwnershipPercentage { get; init; }

    public RwValuationCriterion? Column6ValuationCriterion { get; init; }

    public decimal? Column7InitialValue { get; init; }

    public decimal? Column8FinalValue { get; init; }

    public decimal? Column9MaximumCurrentAccountValueInNonCooperativeCountries { get; init; }

    public int? Column10IvafeOrIcHoldingDays { get; init; }

    public int? Column11IvieHoldingMonths { get; init; }

    public decimal? Column12ForeignTaxCredit { get; init; }

    public decimal? Column13IvieDeduction { get; init; }

    public int? Column14IncomeScheduleCode { get; init; }

    public decimal? Column15ParticipationPercentage { get; init; }

    public bool Column16MonitoringOnly { get; init; }

    public string? Column17BeneficialOwnerEntityTaxCode { get; init; }

    public string? Column18CoOwnerTaxCode { get; init; }

    public string? Column19CoOwnerTaxCode { get; init; }

    public bool Column20MoreThanTwoCoOwners { get; init; }

    public bool Column21PrivilegedTaxRegime { get; init; }

    public decimal? Column29Ivafe { get; init; }

    public decimal? Column30IvafeDue { get; init; }

    public decimal? Column31Ivie { get; init; }

    public decimal? Column32IvieDue { get; init; }

    public decimal? Column33Ic { get; init; }

    public decimal? Column34IcDue { get; init; }
}
