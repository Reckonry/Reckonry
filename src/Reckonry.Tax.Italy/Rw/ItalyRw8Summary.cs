namespace Reckonry.Tax.Italy.Rw;

public sealed record ItalyRw8Summary
{
    public decimal Column1TotalTaxDue { get; init; }

    public decimal Column2PreviousDeclarationExcess { get; init; }

    public decimal Column3F24CompensatedExcess { get; init; }

    public decimal Column4AdvancesPaid { get; init; }

    public decimal Column5TaxDebit { get; init; }

    public decimal Column6TaxCredit { get; init; }
}
