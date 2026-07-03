namespace Reckonry.Reports;

public sealed class AuditReportModule : IReportModule
{
    public ReportDescriptor Descriptor { get; } = new(
        "audit",
        "Ledger Audit",
        ReportScope.Generic,
        CountryCode: null,
        ProviderId: null,
        ProfessionalReviewRequired: false,
        SupportedOutputFormats: ["json", "md"]);
}
