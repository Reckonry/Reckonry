namespace LedgerForge.Tax.Italy.Rw;

public sealed record ItalyRwAssetValuation(
    string AssetSymbol,
    RwValuationCriterion ValuationCriterion,
    RwValuationEvidence InitialValue,
    RwValuationEvidence FinalValue);
