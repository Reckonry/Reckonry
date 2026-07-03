namespace Reckonry.Reports;

public sealed class LedgerReportModule : IReportModule
{
    public ReportDescriptor Descriptor { get; } = new(
        "ledger",
        "Canonical Ledger",
        ReportScope.Generic,
        CountryCode: null,
        ProviderId: null,
        ProfessionalReviewRequired: false,
        SupportedOutputFormats: ["json", "csv"]);
}
