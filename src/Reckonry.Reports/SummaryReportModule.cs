namespace Reckonry.Reports;

public sealed class SummaryReportModule : IReportModule
{
    public ReportDescriptor Descriptor { get; } = new(
        "summary",
        "Ledger Summary",
        ReportScope.Generic,
        CountryCode: null,
        ProviderId: null,
        ProfessionalReviewRequired: false,
        SupportedOutputFormats: ["json", "csv", "md"]);
}
