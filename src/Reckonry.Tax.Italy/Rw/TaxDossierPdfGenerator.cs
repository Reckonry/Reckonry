using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;
using Reckonry.Reports;

namespace Reckonry.Tax.Italy.Rw;

public sealed partial class TaxDossierPdfGenerator : ITaxDossierPdfGenerator, IReportModule
{
    private const string OutputFileNameFormat = "Reckonry-Tax-Dossier-{0}.pdf";
    private const string AccentColor = "F97316";
    private const string BlueColor = "3B82F6";
    private const string DarkColor = "0B0F14";
    private const string GreenColor = "22C55E";
    private const string RedColor = "EF4444";
    private const string YellowColor = "F59E0B";
    private const string GrayColor = "8B99A8";
    private const string LightBorderColor = "D9E2EC";

    public ReportDescriptor Descriptor { get; } = new(
        "italy-tax-dossier",
        "Italy Tax Dossier",
        ReportScope.Professional,
        CountryCode: "IT",
        ProviderId: null,
        ProfessionalReviewRequired: true,
        SupportedOutputFormats: ["pdf"]);

    public async Task<TaxDossierPdfResult> GenerateAsync(
        TaxDossierPdfRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.LedgerJsonPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AccountantHandoffJsonPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AccountantRwJsonPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OutputFolder);

        var dossier = await BuildDossierAsync(request, cancellationToken);
        Directory.CreateDirectory(request.OutputFolder);

        var outputPath = Path.Combine(request.OutputFolder, string.Format(OutputFileNameFormat, request.Year));

        QuestPDF.Settings.License = LicenseType.Community;
        Document
            .Create(container => ComposeDocument(container, dossier))
            .GeneratePdf(outputPath);

        return new TaxDossierPdfResult(
            Path.GetFileName(outputPath),
            dossier.ReadinessStatus,
            dossier.Localizer.Language,
            dossier.Title,
            dossier.PortfolioComposition.Count,
            dossier.MovementTimeline.Count(month => month.EventCount > 0),
            dossier.SourceFiles.Count,
            dossier.ImportedRowCount,
            dossier.LedgerEventCount,
            dossier.UnknownEventCount,
            dossier.OfficialReportDocumentCount,
            dossier.MissingValuationEvidenceCount,
            dossier.ValidationErrors.Count,
            dossier.Warnings.Count);
    }

}
