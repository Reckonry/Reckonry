namespace Reckonry.Reports;

public sealed record ReportDescriptor(
    string Id,
    string DisplayName,
    ReportScope Scope,
    string? CountryCode,
    string? ProviderId,
    bool ProfessionalReviewRequired,
    IReadOnlyList<string> SupportedOutputFormats);
