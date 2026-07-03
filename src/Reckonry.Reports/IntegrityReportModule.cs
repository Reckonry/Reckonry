namespace Reckonry.Reports;

public sealed class IntegrityReportModule : IReportModule
{
    public ReportDescriptor Descriptor { get; } = new(
        "integrity",
        "Ledger Integrity",
        ReportScope.Generic,
        CountryCode: null,
        ProviderId: null,
        ProfessionalReviewRequired: false,
        SupportedOutputFormats: ["json", "md"]);
}
