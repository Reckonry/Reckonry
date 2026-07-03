using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;
using Reckonry.Reports;

namespace Reckonry.Tax.Italy.Rw;

public sealed partial class TaxDossierPdfGenerator
{
    private sealed record TaxDossierViewModel(
        int Year,
        DateTimeOffset GeneratedAtUtc,
        string ReadinessStatus,
        string Title,
        ITextLocalizer Localizer,
        string LedgerHashSha256,
        string GitCommit,
        string ReckonryVersion,
        string? RepositoryUrl,
        string? LogoSvg,
        string QrSvg,
        string QrPayload,
        int ImportedRowCount,
        int LedgerEventCount,
        int UnknownEventCount,
        int OfficialReportDocumentCount,
        int AssetsDetectedCount,
        int MissingValuationEvidenceCount,
        int FilledValuationEvidenceCount,
        IReadOnlyList<DossierSourceFile> SourceFiles,
        DossierReconciliation Reconciliation,
        IReadOnlyList<DossierValidationMessage> ValidationErrors,
        IReadOnlyList<DossierValidationMessage> Warnings,
        IReadOnlyList<string> AccountantChecklist,
        IReadOnlyList<PortfolioAsset> PortfolioComposition,
        IReadOnlyList<MonthlyEventCount> MovementTimeline)
    {
        public string ShortLedgerHash => LedgerHashSha256.Length <= 12
            ? LedgerHashSha256
            : LedgerHashSha256[..12];
    }

    private sealed record DossierSectionLink(
        string Id,
        string Title);

    private sealed record PortfolioAsset(
        string AssetSymbol,
        decimal Value);

    private sealed record MonthlyEventCount(
        string Label,
        int EventCount);

    private sealed record DossierSourceFile(
        string SourceSystem,
        string SourceFile,
        int ImportedRowCount,
        int EventCount,
        int UnknownEventCount);

    private sealed record DossierReconciliation(
        string Status,
        bool OfficialReportsAvailable,
        IReadOnlyList<string> ReportTypes);

    private sealed record DossierValidationMessage(
        string Severity,
        string Code);
}
