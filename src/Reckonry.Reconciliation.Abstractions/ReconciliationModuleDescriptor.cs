namespace Reckonry.Reconciliation.Abstractions;

public sealed record ReconciliationModuleDescriptor(
    string Id,
    string DisplayName,
    ReconciliationScope Scope,
    string? ProviderId,
    string? CountryCode,
    bool ProfessionalReviewRequired,
    IReadOnlyList<string> SupportedInputFormats,
    IReadOnlyList<string> GeneratedArtifacts);
