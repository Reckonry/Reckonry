namespace Reckonry.Tax.Abstractions;

public sealed record TaxModuleDescriptor(
    string CountryCode,
    string DisplayName,
    string Version)
{
    public string CountryName { get; init; } = DisplayName;

    public IReadOnlyList<int> SupportedTaxYears { get; init; } = [];

    public IReadOnlyList<TaxOfficialSource> OfficialSources { get; init; } = [];

    public IReadOnlyList<TaxRequiredInput> RequiredInputs { get; init; } = [];

    public IReadOnlyList<TaxGeneratedArtifact> GeneratedArtifacts { get; init; } = [];

    public IReadOnlyList<TaxConfigurationSchema> ConfigurationSchemas { get; init; } = [];

    public TaxCompatibility Compatibility { get; init; } = new(
        "reckonry-ledger-v1",
        "0.1.0-alpha",
        []);

    public ProfessionalReviewStatus ProfessionalReviewStatus { get; init; } = ProfessionalReviewStatus.Required;
}
