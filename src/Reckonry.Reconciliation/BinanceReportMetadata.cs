namespace Reckonry.Reconciliation;

public sealed record BinanceReportMetadata(
    BinanceReportType ReportType,
    int? TaxYear,
    string DocumentLanguage,
    int PageCount,
    bool IsImageOnly,
    bool ExtractionSucceeded);
