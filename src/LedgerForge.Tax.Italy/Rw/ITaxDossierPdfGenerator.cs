namespace LedgerForge.Tax.Italy.Rw;

public interface ITaxDossierPdfGenerator
{
    Task<TaxDossierPdfResult> GenerateAsync(
        TaxDossierPdfRequest request,
        CancellationToken cancellationToken = default);
}
