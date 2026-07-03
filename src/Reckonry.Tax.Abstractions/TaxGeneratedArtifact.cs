namespace Reckonry.Tax.Abstractions;

public sealed record TaxGeneratedArtifact(
    string Id,
    string DisplayName,
    string OutputFormat,
    bool ProfessionalReviewRequired);
