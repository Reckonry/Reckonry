using Reckonry.Tax.Abstractions;

namespace Reckonry.Tax.Italy;

public sealed class ItalyTaxModule : ITaxModule
{
    public TaxModuleDescriptor Descriptor { get; } = new("IT", "Italy", "0.1.0")
    {
        CountryName = "Italy",
        SupportedTaxYears = [2025, 2026],
        OfficialSources =
        [
            new(
                "Modello Redditi Persone Fisiche and instructions",
                "Agenzia delle Entrate",
                null,
                "2026",
                null)
        ],
        RequiredInputs =
        [
            new("ledger", "Canonical ledger", "Reckonry canonical ledger events.", true),
            new("rw-config", "Italy RW configuration", "Private country configuration reviewed by the user or accountant.", true),
            new("official-reports", "Official provider reports", "Optional official reports used as reconciliation evidence.", false)
        ],
        GeneratedArtifacts =
        [
            new("italy-rw", "Italy RW draft", "json", true),
            new("italy-accountant-package", "Italy accountant package", "json", true),
            new("italy-tax-dossier", "Italy Tax Dossier", "pdf", true)
        ],
        ConfigurationSchemas =
        [
            new("italy-rw-config", "Italy RW configuration", "json", null)
        ],
        Compatibility = new(
            "reckonry-ledger-v1",
            "0.1.0-alpha",
            ["Consumes canonical ledger events only. Does not mutate ledger data."]),
        ProfessionalReviewStatus = ProfessionalReviewStatus.Required
    };

    public TaxReportResult Analyze(TaxReportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new TaxReportResult(
            Descriptor,
            request.Year,
            new[]
            {
                "Italy tax module is a placeholder. It does not calculate taxes, capital gains, LIFO, FIFO, or legal advice."
            });
    }
}
