using Reckonry.Core;
using Reckonry.Tax.Abstractions;

namespace SampleTaxModule;

public sealed class ExampleTaxModulePlugin : ITaxModule
{
    public TaxModuleDescriptor Descriptor { get; } = new("XX", "Example Country", "0.1.0-alpha")
    {
        CountryName = "Example Country",
        SupportedTaxYears = [2025],
        OfficialSources =
        [
            new(
                "Example official tax instructions",
                "Example Revenue Authority",
                "https://example.invalid/official-source",
                "2025",
                null)
        ],
        RequiredInputs =
        [
            new("ledger", "Canonical ledger", "Reckonry canonical ledger events.", true),
            new("professional-config", "Professional configuration", "Country-specific settings reviewed by a qualified professional.", true)
        ],
        GeneratedArtifacts =
        [
            new("example-country-review", "Example country review summary", "md", true)
        ],
        ConfigurationSchemas =
        [
            new("example-country-config", "Example country configuration", "json", null)
        ],
        Compatibility = new(
            "reckonry-ledger-v1",
            "0.1.0-alpha",
            ["Template module. Does not mutate ledger data."]),
        ProfessionalReviewStatus = ProfessionalReviewStatus.Required
    };

    public TaxReportResult Analyze(TaxReportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var warnings = new List<string>
        {
            "Template tax module does not calculate tax, capital gains, filing values, or legal conclusions.",
            "Official sources and professional configuration must be supplied before producing real outputs."
        };

        if (request.LedgerEvents.Any(e => e.EventType == LedgerEventType.Unknown))
        {
            warnings.Add("Unknown ledger events are present and must be reviewed before tax interpretation.");
        }

        return new TaxReportResult(Descriptor, request.Year, warnings);
    }
}
