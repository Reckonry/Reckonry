using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;

namespace Reckonry.Tax.Italy.Rw;

public sealed class TaxDossierPdfGenerator : ITaxDossierPdfGenerator
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

    private static async Task<TaxDossierViewModel> BuildDossierAsync(
        TaxDossierPdfRequest request,
        CancellationToken cancellationToken)
    {
        var localizer = DictionaryTextLocalizer.Create(request.Language, ReportLanguages.Italian);
        using var handoff = await ReadJsonAsync(request.AccountantHandoffJsonPath, cancellationToken);
        using var accountant = await ReadJsonAsync(request.AccountantRwJsonPath, cancellationToken);

        var handoffRoot = handoff.RootElement;
        var accountantRoot = accountant.RootElement;

        var counts = handoffRoot.GetProperty("counts");
        var report = accountantRoot.GetProperty("report");

        var validationMessages = report.TryGetProperty("validationMessages", out var messages)
            ? messages.EnumerateArray().Select(ReadValidationMessage).ToArray()
            : Array.Empty<DossierValidationMessage>();

        var validationErrors = validationMessages
            .Where(message => string.Equals(message.Severity, "Error", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var warnings = validationMessages
            .Where(message => string.Equals(message.Severity, "Warning", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var sourceFiles = handoffRoot.TryGetProperty("sourceFilesSummary", out var sources)
            ? sources.EnumerateArray().Select(ReadSourceFile).ToArray()
            : Array.Empty<DossierSourceFile>();

        var reconciliation = ReadReconciliation(handoffRoot);
        var checklist = handoffRoot.TryGetProperty("accountantChecklist", out var checklistElement)
            ? checklistElement.EnumerateArray()
                .Select(item => LocalizeChecklistItem(localizer, GetString(item, "item", "Review item")))
                .ToArray()
            : DefaultChecklist(localizer);

        var gitCommit = string.IsNullOrWhiteSpace(request.GitCommit) ? "Unknown" : request.GitCommit;
        var version = string.IsNullOrWhiteSpace(request.ReckonryVersion)
            ? Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown"
            : request.ReckonryVersion;
        var ledgerHash = await ComputeSha256Async(request.LedgerJsonPath, cancellationToken);
        var generatedAtUtc = DateTimeOffset.UtcNow;
        var movementTimeline = await ReadMovementTimelineAsync(request.LedgerJsonPath, request.Year, cancellationToken);
        var portfolioComposition = ReadPortfolioComposition(report);
        var qrPayload = BuildVerificationQrPayload(
            request.RepositoryUrl,
            ledgerHash,
            version,
            gitCommit,
            generatedAtUtc);

        return new TaxDossierViewModel(
            request.Year,
            generatedAtUtc,
            localizer.Text("Status.NotReadyForFiling"),
            localizer.Text("Dossier.Title"),
            localizer,
            ledgerHash,
            gitCommit,
            version,
            request.RepositoryUrl,
            await ReadLogoAsync(request.LogoSvgPath, cancellationToken),
            BuildQrSvg(qrPayload),
            qrPayload,
            GetInt(counts, "importedRowCount"),
            GetInt(counts, "ledgerEventCount"),
            GetInt(counts, "unknownEventCount"),
            GetInt(counts, "officialReportDocumentCount"),
            GetInt(counts, "assetsDetectedCount"),
            GetInt(counts, "missingValuationEvidenceCount"),
            GetInt(counts, "filledValuationEvidenceCount"),
            sourceFiles,
            reconciliation,
            validationErrors,
            warnings,
            checklist,
            portfolioComposition,
            movementTimeline);
    }

    private static void ComposeDocument(IDocumentContainer container, TaxDossierViewModel dossier)
    {
        ComposeCoverPage(container, dossier);
        ComposeContentPage(container, dossier, dossier.Localizer.Text("Section.TableOfContents"), "toc", ComposeTableOfContents);
        ComposeContentPage(container, dossier, dossier.Localizer.Text("Section.ExecutiveSummary"), "executive-summary", ComposeExecutiveSummary);
        ComposeContentPage(container, dossier, dossier.Localizer.Text("Section.LedgerIntegrity"), "ledger-integrity", ComposeLedgerIntegrity);
        ComposeContentPage(container, dossier, dossier.Localizer.Text("Section.BinanceReconciliation"), "binance-reconciliation", ComposeBinanceReconciliation);
        ComposeContentPage(container, dossier, dossier.Localizer.Text("Section.SourceDocuments"), "source-documents", ComposeSourceDocuments);
        ComposeContentPage(container, dossier, dossier.Localizer.Text("Section.PortfolioComposition"), "portfolio-composition", ComposePortfolioComposition);
        ComposeContentPage(container, dossier, dossier.Localizer.Text("Section.MovementTimeline"), "movement-timeline", ComposeMovementTimeline);
        ComposeContentPage(container, dossier, dossier.Localizer.Text("Section.RwDraft"), "rw-draft", ComposeRwDraft);
        ComposeContentPage(container, dossier, dossier.Localizer.Text("Section.Rw8Draft"), "rw8-draft", ComposeRw8Draft);
        ComposeContentPage(container, dossier, dossier.Localizer.Text("Section.ValidationMissingInputs"), "validation-missing-inputs", ComposeValidationAndMissingInputs);
        ComposeContentPage(container, dossier, dossier.Localizer.Text("Section.AccountantChecklist"), "professional-checklist", ComposeAccountantChecklist);
        ComposeContentPage(container, dossier, dossier.Localizer.Text("Section.TechnicalAppendix"), "technical-appendix", ComposeTechnicalAppendix);
    }

    private static void ComposeCoverPage(IDocumentContainer container, TaxDossierViewModel dossier)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(0);
            page.DefaultTextStyle(TextStyle.Default.FontFamily("Helvetica").FontSize(10).FontColor(Color.FromHex("111827")));
            page.PageColor(Colors.White);

            page.Content().Column(pageColumn =>
            {
                pageColumn.Item().Height(560).Background(Color.FromHex(DarkColor)).Padding(34).Column(column =>
                {
                    column.Spacing(20);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Height(48).Element(element => ComposeLogo(element, dossier, darkBackground: true));
                    });

                    column.Item().PaddingTop(28).Element(element => ComposeCoverTitle(element, dossier));

                    column.Item().Text(dossier.Localizer.Text("Dossier.PreparedForReview"))
                        .FontSize(16)
                        .FontColor(Color.FromHex("F8FAFC"));

                    column.Item().Width(42).BorderBottom(4).BorderColor(Color.FromHex(AccentColor));

                    column.Item().PaddingTop(8).Column(metadata =>
                    {
                        metadata.Spacing(16);
                        metadata.Item().Element(element => ComposeCoverField(element, dossier.Localizer.Text("Label.Year").ToUpperInvariant(), dossier.Year.ToString()));
                        metadata.Item().Element(element => ComposeCoverField(element, dossier.Localizer.Text("Label.GeneratedUtc").ToUpperInvariant(), FormatCoverDate(dossier)));
                        metadata.Item().Element(element => ComposeCoverField(element, dossier.Localizer.Text("Label.Status").ToUpperInvariant(), dossier.ReadinessStatus, DossierStatusKind.Error));
                    });

                });

                pageColumn.Item().Height(225).PaddingHorizontal(32).PaddingVertical(18).Column(column =>
                {
                    column.Spacing(14);
                    column.Item().Text(dossier.Localizer.Text("Dossier.CoverBody"))
                        .FontSize(8)
                        .LineHeight(1.2f)
                        .FontColor(Color.FromHex("536171"));
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Element(element => ComposeCoverMetric(element, dossier.Localizer.Text("Label.ShortLedgerHash"), dossier.ShortLedgerHash));
                        row.RelativeItem().Element(element => ComposeCoverMetric(element, dossier.Localizer.Text("Label.ReckonryVersion"), dossier.ReckonryVersion));
                        row.RelativeItem().Element(element => ComposeCoverMetric(element, dossier.Localizer.Text("Label.GitCommit"), dossier.GitCommit));
                        row.ConstantItem(92).AlignRight().Element(element => ComposeQrCode(element, dossier));
                    });
                });

                pageColumn.Item().Height(50).Background(Color.FromHex(DarkColor)).PaddingHorizontal(32).Row(row =>
                {
                    row.RelativeItem().AlignMiddle().Text(dossier.Localizer.Text("Footer.GeneratedBy"))
                        .FontSize(9)
                        .FontColor(Colors.White);
                    row.RelativeItem().AlignMiddle().AlignCenter().Text(dossier.RepositoryUrl ?? "https://github.com/reckonry/reckonry")
                        .FontSize(8)
                        .FontColor(Color.FromHex("9AA7B5"));
                    row.RelativeItem().AlignMiddle().AlignRight().Text($"{dossier.Localizer.Text("Footer.Page")}1")
                        .FontSize(8)
                        .FontColor(Colors.White);
                });
            });
        });
    }

    private static void ComposeContentPage(
        IDocumentContainer container,
        TaxDossierViewModel dossier,
        string title,
        string sectionId,
        Action<IContainer, TaxDossierViewModel> content)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(TextStyle.Default.FontFamily("Helvetica").FontSize(9).FontColor(Color.FromHex("111827")));
            page.PageColor(Colors.White);

            page.Header().Element(element => ComposeHeader(element, title, dossier));
            page.Content().Section(sectionId).PaddingVertical(18).Element(element => content(element, dossier));
            page.Footer().Element(element => ComposeFooter(element, dossier));
        });
    }

    private static void ComposeHeader(IContainer container, string sectionTitle, TaxDossierViewModel dossier)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("RECKONRY").FontSize(8).SemiBold().FontColor(Color.FromHex(DarkColor));
                column.Item().Text(sectionTitle).FontSize(7).FontColor(Color.FromHex("536171"));
            });
            row.ConstantItem(180).AlignRight().Text($"{dossier.Title} {dossier.Year}").FontSize(7).FontColor(Color.FromHex("536171"));
        });
    }

    private static void ComposeFooter(IContainer container, TaxDossierViewModel dossier)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(dossier.Localizer.Text("Footer.GeneratedBy"))
                    .FontSize(8)
                    .SemiBold()
                    .FontColor(Color.FromHex(AccentColor));
                column.Item().Text($"{dossier.Title} | {dossier.Localizer.Text("Label.ShortLedgerHash")}: {dossier.ShortLedgerHash}")
                    .FontSize(7)
                    .FontColor(Color.FromHex("536171"));
            });
            row.ConstantItem(140).Text(dossier.Localizer.Text("Footer.NotTaxFiling"))
                .FontSize(8)
                .FontColor(Color.FromHex("536171"));
            row.ConstantItem(80).AlignRight().Text(text =>
            {
                text.Span(dossier.Localizer.Text("Footer.Page")).FontSize(8).FontColor(Color.FromHex("536171"));
                text.CurrentPageNumber().FontSize(8).FontColor(Color.FromHex("536171"));
            });
        });
    }

    private static void ComposeTableOfContents(IContainer container, TaxDossierViewModel dossier)
    {
        var sections = new[]
        {
            new DossierSectionLink("executive-summary", dossier.Localizer.Text("Section.ExecutiveSummary")),
            new DossierSectionLink("ledger-integrity", dossier.Localizer.Text("Section.LedgerIntegrity")),
            new DossierSectionLink("binance-reconciliation", dossier.Localizer.Text("Section.BinanceReconciliation")),
            new DossierSectionLink("source-documents", dossier.Localizer.Text("Section.SourceDocuments")),
            new DossierSectionLink("portfolio-composition", dossier.Localizer.Text("Section.PortfolioComposition")),
            new DossierSectionLink("movement-timeline", dossier.Localizer.Text("Section.MovementTimeline")),
            new DossierSectionLink("rw-draft", dossier.Localizer.Text("Section.RwDraft")),
            new DossierSectionLink("rw8-draft", dossier.Localizer.Text("Section.Rw8Draft")),
            new DossierSectionLink("validation-missing-inputs", dossier.Localizer.Text("Section.ValidationMissingInputs")),
            new DossierSectionLink("professional-checklist", dossier.Localizer.Text("Section.AccountantChecklist")),
            new DossierSectionLink("technical-appendix", dossier.Localizer.Text("Section.TechnicalAppendix"))
        };

        container.Column(column =>
        {
            column.Spacing(11);
            column.Item().Text(dossier.Localizer.Text("Section.TableOfContents").ToUpperInvariant()).FontSize(20).SemiBold();
            column.Item().Width(30).BorderBottom(3).BorderColor(Color.FromHex(AccentColor));
            for (var index = 0; index < sections.Length; index++)
            {
                var section = sections[index];
                column.Item().SectionLink(section.Id).Row(row =>
                {
                    row.ConstantItem(22).Height(22).Background(Color.FromHex(DarkColor)).AlignCenter().AlignMiddle().Text((index + 1).ToString()).FontSize(8).FontColor(Colors.White).SemiBold();
                    row.ConstantItem(12);
                    row.AutoItem().AlignMiddle().Text(section.Title).FontSize(10).SemiBold();
                    row.RelativeItem().PaddingHorizontal(8).AlignMiddle().BorderBottom(1).BorderColor(Color.FromHex(LightBorderColor));
                    row.ConstantItem(24).AlignMiddle().AlignRight().Text(">").FontSize(9).FontColor(Color.FromHex("536171"));
                });
            }
        });
    }

    private static void ComposeExecutiveSummary(IContainer container, TaxDossierViewModel dossier)
    {
        container.Column(column =>
        {
            column.Spacing(14);
            column.Item().Text($"1. {dossier.Localizer.Text("Section.ExecutiveSummary").ToUpperInvariant()}").FontSize(16).SemiBold();
            column.Item().Text(dossier.Localizer.Text("Text.ExecutiveSummary"))
                .FontSize(11)
                .LineHeight(1.35f);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                AddMetricCard(table, dossier.Localizer.Text("Label.ImportedRows"), dossier.ImportedRowCount.ToString("N0"), BlueColor);
                AddMetricCard(table, dossier.Localizer.Text("Label.LedgerEvents"), dossier.LedgerEventCount.ToString("N0"), GreenColor);
                AddMetricCard(table, dossier.Localizer.Text("Label.UnknownEvents"), dossier.UnknownEventCount.ToString("N0"), dossier.UnknownEventCount == 0 ? GreenColor : YellowColor);
                AddMetricCard(table, dossier.Localizer.Text("Label.OfficialReports"), dossier.OfficialReportDocumentCount.ToString("N0"), BlueColor);
                AddMetricCard(table, dossier.Localizer.Text("Label.AssetsDetected"), dossier.AssetsDetectedCount.ToString("N0"), DarkColor);
                AddMetricCard(table, dossier.Localizer.Text("Label.MissingValuationEvidence"), dossier.MissingValuationEvidenceCount.ToString("N0"), RedColor);
                AddMetricCard(table, dossier.Localizer.Text("Label.ValidationErrors"), dossier.ValidationErrors.Count.ToString("N0"), RedColor);
                AddMetricCard(table, dossier.Localizer.Text("Label.Warnings"), dossier.Warnings.Count.ToString("N0"), YellowColor);
                AddMetricCard(table, dossier.Localizer.Text("Label.Status"), dossier.ReadinessStatus, RedColor, smallValue: true);
            });
        });
    }

    private static void ComposeLedgerIntegrity(IContainer container, TaxDossierViewModel dossier)
    {
        container.Column(column =>
        {
            column.Spacing(12);
            column.Item().Text($"2. {dossier.Localizer.Text("Section.LedgerIntegrity").ToUpperInvariant()}").FontSize(16).SemiBold();
            column.Item().Text(dossier.Localizer.Text("Text.LedgerIntegrity"))
                .LineHeight(1.3f);
            column.Item().Element(element => ComposeIntegrityRows(element, dossier));
            column.Item().PaddingTop(8).Element(element => ComposeOverallStatusCard(
                element,
                dossier.UnknownEventCount == 0 ? dossier.Localizer.Text("Value.Pass") : dossier.Localizer.Text("Value.Warning"),
                dossier.UnknownEventCount == 0 ? DossierStatusKind.Pass : DossierStatusKind.Warning,
                dossier.UnknownEventCount == 0 ? dossier.Localizer.Text("Text.LedgerOverallPass") : dossier.Localizer.Text("Text.LedgerOverallWarning")));
        });
    }

    private static void ComposeBinanceReconciliation(IContainer container, TaxDossierViewModel dossier)
    {
        container.Column(column =>
        {
            column.Spacing(10);
            column.Item().Text($"3. {dossier.Localizer.Text("Section.BinanceReconciliation").ToUpperInvariant()}").FontSize(16).SemiBold();
            column.Item().Text(dossier.Localizer.Text("Text.BinanceReconciliation"))
                .LineHeight(1.3f);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                AddKeyValue(table, dossier.Localizer.Text("Label.Status"), dossier.Reconciliation.Status);
                AddKeyValue(table, dossier.Localizer.Text("Label.OfficialReportsAvailable"), dossier.Reconciliation.OfficialReportsAvailable ? dossier.Localizer.Text("Value.Yes") : dossier.Localizer.Text("Value.No"));
                AddKeyValue(table, dossier.Localizer.Text("Label.OfficialReportDocuments"), dossier.OfficialReportDocumentCount.ToString());
                AddKeyValue(table, dossier.Localizer.Text("Label.ReportTypes"), string.Join(", ", dossier.Reconciliation.ReportTypes));
            });
        });
    }

    private static void ComposeSourceDocuments(IContainer container, TaxDossierViewModel dossier)
    {
        container.Column(column =>
        {
            column.Spacing(10);
            column.Item().Text($"4. {dossier.Localizer.Text("Section.SourceDocuments").ToUpperInvariant()}").FontSize(16).SemiBold();
            column.Item().Text(dossier.Localizer.Text("Text.SourceDocuments"))
                .LineHeight(1.3f);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                AddHeader(table, dossier.Localizer.Text("Label.System"));
                AddHeader(table, dossier.Localizer.Text("Label.File"));
                AddHeader(table, dossier.Localizer.Text("Label.Rows"));
                AddHeader(table, dossier.Localizer.Text("Label.Unknown"));

                foreach (var source in dossier.SourceFiles.Take(24))
                {
                    AddCell(table, source.SourceSystem);
                    AddCell(table, source.SourceFile);
                    AddCell(table, source.ImportedRowCount.ToString());
                    AddCell(table, source.UnknownEventCount.ToString());
                }
            });

            if (dossier.SourceFiles.Count > 24)
            {
                column.Item().Text(dossier.Localizer.Format("Text.AdditionalSourceFiles", dossier.SourceFiles.Count - 24))
                    .FontColor(Colors.Grey.Darken2);
            }
        });
    }

    private static void ComposePortfolioComposition(IContainer container, TaxDossierViewModel dossier)
    {
        container.Column(column =>
        {
            column.Spacing(12);
            column.Item().Text($"5. {dossier.Localizer.Text("Section.PortfolioComposition").ToUpperInvariant()}").FontSize(16).SemiBold();
            column.Item().Text(dossier.Localizer.Text("Text.PortfolioComposition")).LineHeight(1.3f);

            if (dossier.PortfolioComposition.Count == 0)
            {
                column.Item().Element(element => ComposeEmptyState(
                    element,
                    dossier.Localizer.Text("Label.PortfolioChart"),
                    dossier.Localizer.Text("Text.PortfolioEmpty")));
                return;
            }

            column.Item().Element(element => ComposePortfolioDonut(element, dossier));
        });
    }

    private static void ComposeMovementTimeline(IContainer container, TaxDossierViewModel dossier)
    {
        container.Column(column =>
        {
            column.Spacing(12);
            column.Item().Text($"6. {dossier.Localizer.Text("Section.MovementTimeline").ToUpperInvariant()}").FontSize(16).SemiBold();
            column.Item().Text(dossier.Localizer.Text("Text.MovementTimeline")).LineHeight(1.3f);
            column.Item().Height(270).Element(element => ComposeTimelineLineChart(element, dossier));
        });
    }

    private static void ComposePortfolioDonut(IContainer container, TaxDossierViewModel dossier)
    {
        var total = dossier.PortfolioComposition.Sum(asset => asset.Value);
        var palette = new[] { AccentColor, BlueColor, GreenColor, "536171", "111821", YellowColor };

        container.Border(1).BorderColor(Color.FromHex(LightBorderColor)).Padding(16).Row(row =>
        {
            row.ConstantItem(220).Height(186).Svg(BuildPortfolioDonutSvg(dossier.PortfolioComposition, palette));
            row.ConstantItem(18);
            row.RelativeItem().Column(column =>
            {
                column.Spacing(9);
                for (var i = 0; i < dossier.PortfolioComposition.Count; i++)
                {
                    var asset = dossier.PortfolioComposition[i];
                    var percent = total <= 0 ? 0 : Math.Round(asset.Value / total * 100m, 2);
                    var color = palette[i % palette.Length];
                    column.Item().Row(legend =>
                    {
                        legend.ConstantItem(10).Height(10).Background(Color.FromHex(color));
                        legend.ConstantItem(10);
                        legend.RelativeItem().Text(asset.AssetSymbol).FontSize(9).SemiBold();
                        legend.AutoItem().Text($"{percent:0.##}%").FontSize(9).FontColor(Color.FromHex("536171"));
                    });
                }
            });
        });
    }

    private static void ComposeTimelineLineChart(IContainer container, TaxDossierViewModel dossier)
    {
        container.Border(1).BorderColor(Color.FromHex(LightBorderColor)).Padding(14).Svg(BuildTimelineSvg(dossier.MovementTimeline));
    }

    private static void ComposeRwDraft(IContainer container, TaxDossierViewModel dossier)
    {
        container.Column(column =>
        {
            column.Spacing(10);
            column.Item().Text($"7. {dossier.Localizer.Text("Section.RwDraft").ToUpperInvariant()}").FontSize(16).SemiBold();
            column.Item().Text(dossier.Localizer.Text("Text.RwDraft"))
                .LineHeight(1.3f);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                AddKeyValue(table, dossier.Localizer.Text("Label.AssetsDetected"), dossier.AssetsDetectedCount.ToString());
                AddKeyValue(table, dossier.Localizer.Text("Label.FilledValuationEvidence"), dossier.FilledValuationEvidenceCount.ToString());
                AddKeyValue(table, dossier.Localizer.Text("Label.MissingValuationEvidence"), dossier.MissingValuationEvidenceCount.ToString());
                AddKeyValue(table, dossier.Localizer.Text("Label.AutonomousFiling"), dossier.Localizer.Text("Value.No"));
            });
        });
    }

    private static void ComposeRw8Draft(IContainer container, TaxDossierViewModel dossier)
    {
        container.Column(column =>
        {
            column.Spacing(10);
            column.Item().Text($"8. {dossier.Localizer.Text("Section.Rw8Draft").ToUpperInvariant()}").FontSize(16).SemiBold();
            column.Item().Text(dossier.Localizer.Text("Text.Rw8Draft"))
                .LineHeight(1.3f);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                AddKeyValue(table, dossier.Localizer.Text("Label.Status"), dossier.Localizer.Text("Value.Draft"));
                AddKeyValue(table, dossier.Localizer.Text("Label.AccountantReviewRequired"), dossier.Localizer.Text("Value.Yes"));
                AddKeyValue(table, dossier.Localizer.Text("Label.FinalFilingOutput"), dossier.Localizer.Text("Value.No"));
                AddKeyValue(table, dossier.Localizer.Text("Label.TaxAdvice"), dossier.Localizer.Text("Value.No"));
            });
        });
    }

    private static void ComposeValidationAndMissingInputs(IContainer container, TaxDossierViewModel dossier)
    {
        container.Column(column =>
        {
            column.Spacing(10);
            column.Item().Text($"9. {dossier.Localizer.Text("Section.ValidationMissingInputs").ToUpperInvariant()}").FontSize(16).SemiBold();
            column.Item().Row(row =>
            {
                row.AutoItem().Element(element => ComposeStatusBadge(
                    element,
                    dossier.ValidationErrors.Count == 0 ? dossier.Localizer.Text("Value.Pass") : dossier.Localizer.Text("Value.Error"),
                    ResolveStatusKind(dossier.ValidationErrors.Count, 0)));
                row.ConstantItem(10);
                row.AutoItem().Element(element => ComposeStatusBadge(
                    element,
                    dossier.Warnings.Count == 0 ? dossier.Localizer.Text("Value.Pass") : dossier.Localizer.Text("Value.Warning"),
                    ResolveStatusKind(0, dossier.Warnings.Count)));
            });
            column.Item().Text(dossier.Localizer.Text("Text.ValidationMissingInputs"))
                .LineHeight(1.3f);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                AddHeader(table, dossier.Localizer.Text("Label.Code"));
                AddHeader(table, dossier.Localizer.Text("Label.Severity"));
                AddHeader(table, dossier.Localizer.Text("Label.Count"));

                foreach (var group in dossier.ValidationErrors.Concat(dossier.Warnings)
                             .GroupBy(message => new { message.Code, message.Severity })
                             .OrderBy(group => group.Key.Severity)
                             .ThenBy(group => group.Key.Code))
                {
                    AddCell(table, group.Key.Code);
                    AddCell(table, group.Key.Severity);
                    AddCell(table, group.Count().ToString());
                }
            });
        });
    }

    private static void ComposeAccountantChecklist(IContainer container, TaxDossierViewModel dossier)
    {
        container.Column(column =>
        {
            column.Spacing(10);
            column.Item().Text($"10. {dossier.Localizer.Text("Section.AccountantChecklist").ToUpperInvariant()}").FontSize(16).SemiBold();
            foreach (var item in dossier.AccountantChecklist)
            {
                column.Item().Row(row =>
                {
                    row.ConstantItem(14).Text("□").FontColor(Color.FromHex(AccentColor));
                    row.RelativeItem().Text(item).FontSize(10);
                });
            }
        });
    }

    private static void ComposeTechnicalAppendix(IContainer container, TaxDossierViewModel dossier)
    {
        container.Column(column =>
        {
            column.Spacing(10);
            column.Item().Text($"11. {dossier.Localizer.Text("Section.TechnicalAppendix").ToUpperInvariant()}").FontSize(16).SemiBold();
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn(2);
                });

                AddKeyValue(table, dossier.Localizer.Text("Label.LedgerSha256"), dossier.LedgerHashSha256);
                AddKeyValue(table, dossier.Localizer.Text("Label.ReportSha256"), dossier.Localizer.Text("Value.NotAvailable"));
                AddKeyValue(table, dossier.Localizer.Text("Label.ReckonryVersion"), dossier.ReckonryVersion);
                AddKeyValue(table, dossier.Localizer.Text("Label.GitCommit"), dossier.GitCommit);
                AddKeyValue(table, dossier.Localizer.Text("Label.GeneratedUtc"), dossier.GeneratedAtUtc.ToString("O"));
                AddKeyValue(table, dossier.Localizer.Text("Label.InputFileCount"), dossier.SourceFiles.Count.ToString());
                AddKeyValue(table, dossier.Localizer.Text("Label.ImportedRows"), dossier.ImportedRowCount.ToString());
                AddKeyValue(table, dossier.Localizer.Text("Label.UnknownEvents"), dossier.UnknownEventCount.ToString());
                AddKeyValue(table, dossier.Localizer.Text("Label.ValidationErrors"), dossier.ValidationErrors.Count.ToString());
                AddKeyValue(table, dossier.Localizer.Text("Label.Warnings"), dossier.Warnings.Count.ToString());
            });
        });
    }

    private static void ComposeLogo(IContainer container, TaxDossierViewModel dossier, bool darkBackground = false)
    {
        if (!string.IsNullOrWhiteSpace(dossier.LogoSvg))
        {
            container.AlignLeft().Width(190).Svg(dossier.LogoSvg);
            return;
        }

        container.Text("Reckonry")
            .FontSize(22)
            .SemiBold()
            .FontColor(darkBackground ? Colors.White : Color.FromHex(AccentColor));
    }

    private static void ComposeCoverTitle(IContainer container, TaxDossierViewModel dossier)
    {
        var isItalian = dossier.Localizer.Language == ReportLanguages.Italian;
        container.Text(text =>
        {
            if (isItalian)
            {
                text.Span("DOSSIER FISCALE").FontSize(32).SemiBold().FontColor(Colors.White);
                text.Line("");
                text.Span("CRIPTO").FontSize(32).SemiBold().FontColor(Color.FromHex(AccentColor));
                return;
            }

            text.Span("CRYPTO").FontSize(32).SemiBold().FontColor(Color.FromHex(AccentColor));
            text.Line("");
            text.Span("TAX DOSSIER").FontSize(32).SemiBold().FontColor(Colors.White);
        });
    }

    private static string FormatCoverDate(TaxDossierViewModel dossier)
    {
        var culture = CultureInfo.GetCultureInfo(dossier.Localizer.Language);
        return dossier.GeneratedAtUtc.ToString("dd MMMM yyyy", culture);
    }

    private static void ComposeCoverField(
        IContainer container,
        string label,
        string value,
        DossierStatusKind? statusKind = null)
    {
        container.Column(column =>
        {
            column.Spacing(6);
            column.Item().Text(label).FontSize(8).SemiBold().FontColor(Color.FromHex(AccentColor));
            if (statusKind is null)
            {
                column.Item().Text(value).FontSize(15).SemiBold().FontColor(Colors.White);
                return;
            }

            column.Item().Width(250).Element(element => ComposeStatusBadge(element, value, statusKind.Value));
        });
    }

    private static void ComposeStatusBanner(IContainer container, string status)
    {
        container
            .Background(Color.FromHex(AccentColor))
            .PaddingVertical(10)
            .PaddingHorizontal(14)
            .Text(status)
            .FontSize(13)
            .SemiBold()
            .FontColor(Colors.White);
    }

    private static void ComposeStatusBadge(IContainer container, string label, DossierStatusKind statusKind)
    {
        container
            .Background(Color.FromHex(StatusColor(statusKind)))
            .PaddingVertical(6)
            .PaddingHorizontal(10)
            .Text(label)
            .FontSize(9)
            .SemiBold()
            .FontColor(Colors.White);
    }

    private static void ComposeSummaryStatus(
        IContainer container,
        string title,
        string status,
        DossierStatusKind statusKind)
    {
        container.Border(1).BorderColor(Color.FromHex(LightBorderColor)).Padding(12).Column(column =>
        {
            column.Spacing(6);
            column.Item().Text(title).FontSize(8).FontColor(Colors.Grey.Darken2);
            column.Item().Element(element => ComposeStatusBadge(element, status, statusKind));
        });
    }

    private static void ComposeMetricCard(
        IContainer container,
        string title,
        string value,
        string color,
        bool smallValue = false)
    {
        container.Border(1).BorderColor(Color.FromHex(LightBorderColor)).Padding(12).Column(column =>
        {
            column.Spacing(8);
            column.Item().Text(title).FontSize(7).SemiBold().FontColor(Color.FromHex("111827"));
            column.Item().Text(value)
                .FontSize(smallValue ? 9 : 16)
                .SemiBold()
                .FontColor(Color.FromHex(color));
        });
    }

    private static void ComposeIntegrityRows(IContainer container, TaxDossierViewModel dossier)
    {
        var unknownStatus = dossier.UnknownEventCount == 0 ? DossierStatusKind.Pass : DossierStatusKind.Warning;
        var rows = new[]
        {
            (dossier.Localizer.Text("Label.SourceFile"), DossierStatusKind.Pass),
            (dossier.Localizer.Text("Label.ImportedRows"), DossierStatusKind.Pass),
            (dossier.Localizer.Text("Label.UnknownEvents"), unknownStatus),
            (dossier.Localizer.Text("Label.LedgerEvents"), DossierStatusKind.Pass),
            (dossier.Localizer.Text("Label.LedgerSha256"), DossierStatusKind.Pass),
            (dossier.Localizer.Text("Section.BinanceReconciliation"), dossier.Reconciliation.OfficialReportsAvailable ? DossierStatusKind.Pass : DossierStatusKind.Warning)
        };

        container.Border(1).BorderColor(Color.FromHex(LightBorderColor)).Column(column =>
        {
            foreach (var row in rows)
            {
                column.Item().BorderBottom(1).BorderColor(Color.FromHex(LightBorderColor)).Padding(9).Row(line =>
                {
                    line.ConstantItem(14).Height(14).Background(Color.FromHex(StatusColor(row.Item2)));
                    line.ConstantItem(10);
                    line.RelativeItem().Text(row.Item1).FontSize(9);
                    line.AutoItem().Text(StatusText(dossier.Localizer, row.Item2)).FontSize(8).SemiBold().FontColor(Color.FromHex(StatusColor(row.Item2)));
                });
            }
        });
    }

    private static void ComposeOverallStatusCard(
        IContainer container,
        string status,
        DossierStatusKind statusKind,
        string message)
    {
        container.Border(1).BorderColor(Color.FromHex(StatusColor(statusKind))).Background(Color.FromHex("F8FAFC")).Padding(16).Row(row =>
        {
            row.ConstantItem(34).Height(34).Background(Color.FromHex(StatusColor(statusKind)));
            row.ConstantItem(14);
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(status).FontSize(14).SemiBold().FontColor(Color.FromHex(StatusColor(statusKind)));
                column.Item().Text(message).FontSize(9).FontColor(Color.FromHex("536171"));
            });
        });
    }

    private static void ComposeCoverMetric(IContainer container, string label, string value)
    {
        container.BorderBottom(1).BorderColor(Color.FromHex(LightBorderColor)).PaddingBottom(8).Column(column =>
        {
            column.Spacing(4);
            column.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken2);
            column.Item().Text(value).FontSize(10).SemiBold();
        });
    }

    private static void ComposeQrCode(IContainer container, TaxDossierViewModel dossier)
    {
        container.Width(76).Height(76).Background(Colors.White).Padding(5).Svg(dossier.QrSvg);
    }

    private static void ComposeEmptyState(IContainer container, string title, string message)
    {
        container.Border(1).BorderColor(Color.FromHex(LightBorderColor)).Background(Colors.Grey.Lighten5).Padding(18).Column(column =>
        {
            column.Spacing(6);
            column.Item().Element(element => ComposeStatusBadge(element, title, DossierStatusKind.NotApplicable));
            column.Item().Text(message).FontColor(Colors.Grey.Darken2).LineHeight(1.3f);
        });
    }

    private static string StatusColor(DossierStatusKind statusKind)
    {
        return statusKind switch
        {
            DossierStatusKind.Pass => GreenColor,
            DossierStatusKind.Warning => YellowColor,
            DossierStatusKind.Error => RedColor,
            _ => GrayColor
        };
    }

    private static string StatusText(ITextLocalizer localizer, DossierStatusKind statusKind)
    {
        return statusKind switch
        {
            DossierStatusKind.Pass => localizer.Text("Value.Pass"),
            DossierStatusKind.Warning => localizer.Text("Value.Warning"),
            DossierStatusKind.Error => localizer.Text("Value.Error"),
            _ => localizer.Text("Value.NotApplicable")
        };
    }

    private static string BuildPortfolioDonutSvg(
        IReadOnlyList<PortfolioAsset> assets,
        IReadOnlyList<string> palette)
    {
        var total = assets.Sum(asset => asset.Value);
        if (assets.Count == 0 || total <= 0)
        {
            return """
            <svg xmlns="http://www.w3.org/2000/svg" width="260" height="220" viewBox="0 0 260 220">
              <rect width="260" height="220" fill="#FFFFFF"/>
              <circle cx="130" cy="104" r="70" fill="none" stroke="#D9E2EC" stroke-width="34"/>
              <text x="130" y="102" text-anchor="middle" font-family="Arial" font-size="10" font-weight="700" fill="#536171">NO VALUES</text>
              <text x="130" y="118" text-anchor="middle" font-family="Arial" font-size="9" fill="#8B99A8">available</text>
            </svg>
            """;
        }

        var builder = new StringBuilder();
        builder.Append("""
            <svg xmlns="http://www.w3.org/2000/svg" width="260" height="220" viewBox="0 0 260 220">
              <rect width="260" height="220" fill="#FFFFFF"/>
              <circle cx="130" cy="104" r="70" fill="none" stroke="#EEF2F7" stroke-width="34"/>
            """);

        var circumference = 439.82m;
        var offset = 0m;
        for (var i = 0; i < assets.Count; i++)
        {
            var ratio = assets[i].Value / total;
            var dash = Math.Round(circumference * ratio, 2);
            var gap = Math.Max(0, circumference - dash);
            builder.Append(CultureInvariant($"""
              <circle cx="130" cy="104" r="70" fill="none" stroke="#{palette[i % palette.Count]}" stroke-width="34" stroke-dasharray="{dash} {gap}" stroke-dashoffset="-{offset}" transform="rotate(-90 130 104)" stroke-linecap="butt"/>
            """));
            offset += dash;
        }

        builder.Append("""
              <circle cx="130" cy="104" r="45" fill="#FFFFFF"/>
              <text x="130" y="96" text-anchor="middle" font-family="Arial" font-size="9" font-weight="700" fill="#111827">TOTAL</text>
              <text x="130" y="112" text-anchor="middle" font-family="Arial" font-size="8" fill="#536171">available</text>
              <text x="130" y="128" text-anchor="middle" font-family="Arial" font-size="9" font-weight="700" fill="#111827">evidence</text>
            </svg>
            """);

        return builder.ToString();
    }

    private static string BuildTimelineSvg(IReadOnlyList<MonthlyEventCount> months)
    {
        var max = Math.Max(1, months.Max(month => month.EventCount));
        var points = new List<(decimal X, decimal Y, int Count)>();
        for (var i = 0; i < months.Count; i++)
        {
            var x = 28m + i * 38m;
            var y = 170m - (months[i].EventCount / (decimal)max * 130m);
            points.Add((x, y, months[i].EventCount));
        }

        var pointString = string.Join(" ", points.Select(point => CultureInvariant($"{point.X:0.##},{point.Y:0.##}")));
        var builder = new StringBuilder();
        builder.Append("""
            <svg xmlns="http://www.w3.org/2000/svg" width="500" height="220" viewBox="0 0 500 220">
              <rect width="500" height="220" fill="#FFFFFF"/>
              <line x1="28" y1="40" x2="470" y2="40" stroke="#EEF2F7"/>
              <line x1="28" y1="83" x2="470" y2="83" stroke="#EEF2F7"/>
              <line x1="28" y1="126" x2="470" y2="126" stroke="#EEF2F7"/>
              <line x1="28" y1="170" x2="470" y2="170" stroke="#D9E2EC"/>
            """);
        builder.Append(CultureInvariant($"""<polyline points="{pointString}" fill="none" stroke="#F97316" stroke-width="4" stroke-linecap="round" stroke-linejoin="round"/>"""));

        for (var i = 0; i < points.Count; i++)
        {
            builder.Append(CultureInvariant($"""<circle cx="{points[i].X:0.##}" cy="{points[i].Y:0.##}" r="4" fill="#F97316"/>"""));
            builder.Append(CultureInvariant($"""<text x="{points[i].X:0.##}" y="194" text-anchor="middle" font-family="Arial" font-size="8" fill="#536171">{months[i].Label}</text>"""));
        }

        builder.Append("""
            </svg>
            """);
        return builder.ToString();
    }

    private static string CultureInvariant(FormattableString value)
    {
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void AddKeyValue(TableDescriptor table, string key, string value)
    {
        AddCell(table, key, true);
        AddCell(table, value);
    }

    private static void AddMetricCard(
        TableDescriptor table,
        string title,
        string value,
        string color,
        bool smallValue = false)
    {
        table.Cell().Padding(5).Element(element => ComposeMetricCard(element, title, value, color, smallValue));
    }

    private static void AddHeader(TableDescriptor table, string text)
    {
        table.Cell()
            .Background(Color.FromHex(AccentColor))
            .Padding(5)
            .Text(text)
            .FontColor(Colors.White)
            .SemiBold();
    }

    private static void AddCell(TableDescriptor table, string text, bool strong = false)
    {
        var cell = table.Cell()
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(5);

        var textBlock = cell.Text(text);
        if (strong)
        {
            textBlock.SemiBold();
        }
    }

    private static async Task<JsonDocument> ReadJsonAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static async Task<string?> ReadLogoAsync(string? logoSvgPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(logoSvgPath) || !File.Exists(logoSvgPath))
        {
            return null;
        }

        return await File.ReadAllTextAsync(logoSvgPath, cancellationToken);
    }

    private static DossierValidationMessage ReadValidationMessage(JsonElement element)
    {
        return new DossierValidationMessage(
            GetString(element, "severity", "Unknown"),
            GetString(element, "code", "Unknown"));
    }

    private static DossierSourceFile ReadSourceFile(JsonElement element)
    {
        return new DossierSourceFile(
            GetString(element, "sourceSystem", "Unknown"),
            GetString(element, "sourceFile", "Unknown"),
            GetInt(element, "importedRowCount"),
            GetInt(element, "eventCount"),
            GetInt(element, "unknownEventCount"));
    }

    private static DossierReconciliation ReadReconciliation(JsonElement handoffRoot)
    {
        if (!handoffRoot.TryGetProperty("reconciliationStatus", out var reconciliation))
        {
            return new DossierReconciliation("Unknown", false, Array.Empty<string>());
        }

        var reportTypes = reconciliation.TryGetProperty("reportTypes", out var reportTypesElement)
            && reportTypesElement.ValueKind == JsonValueKind.Array
                ? reportTypesElement.EnumerateArray().Select(item => item.GetString() ?? "Unknown").ToArray()
                : Array.Empty<string>();

        return new DossierReconciliation(
            GetString(reconciliation, "status", "Unknown"),
            GetBool(reconciliation, "officialReportsAvailable"),
            reportTypes);
    }

    private static async Task<IReadOnlyList<MonthlyEventCount>> ReadMovementTimelineAsync(
        string ledgerJsonPath,
        int year,
        CancellationToken cancellationToken)
    {
        using var document = await ReadJsonAsync(ledgerJsonPath, cancellationToken);
        var counts = new int[12];

        var events = document.RootElement.ValueKind == JsonValueKind.Array
            ? document.RootElement
            : document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("events", out var nestedEvents)
                && nestedEvents.ValueKind == JsonValueKind.Array
                    ? nestedEvents
                    : default;

        if (events.ValueKind == JsonValueKind.Array)
        {
            foreach (var ledgerEvent in events.EnumerateArray())
            {
                if (!ledgerEvent.TryGetProperty("timestampUtc", out var timestamp)
                    || timestamp.ValueKind != JsonValueKind.String
                    || !DateTimeOffset.TryParse(timestamp.GetString(), out var parsed)
                    || parsed.UtcDateTime.Year != year)
                {
                    continue;
                }

                counts[parsed.UtcDateTime.Month - 1]++;
            }
        }

        return Enumerable.Range(1, 12)
            .Select(month => new MonthlyEventCount(MonthLabel(month), counts[month - 1]))
            .ToArray();
    }

    private static IReadOnlyList<PortfolioAsset> ReadPortfolioComposition(JsonElement report)
    {
        if (!report.TryGetProperty("cryptoLines", out var lines)
            || lines.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<PortfolioAsset>();
        }

        return lines.EnumerateArray()
            .Select(ReadPortfolioAsset)
            .Where(asset => asset is not null)
            .Select(asset => asset!)
            .OrderByDescending(asset => asset.Value)
            .ThenBy(asset => asset.AssetSymbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static PortfolioAsset? ReadPortfolioAsset(JsonElement line)
    {
        var assetSymbol = GetString(line, "assetSymbol", string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(assetSymbol)
            || !line.TryGetProperty("finalValueEvidence", out var evidence)
            || evidence.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
            || !TryGetDecimal(line, "column8FinalValue", out var finalValue)
            || finalValue <= 0)
        {
            return null;
        }

        return new PortfolioAsset(assetSymbol, finalValue);
    }

    public static string BuildVerificationQrPayload(
        string? repositoryUrl,
        string ledgerHash,
        string reckonryVersion,
        string gitCommit,
        DateTimeOffset generatedAtUtc)
    {
        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(repositoryUrl))
        {
            builder.Append("repository=").Append(repositoryUrl.Trim()).Append('\n');
        }

        builder
            .Append("ledger_sha256=").Append(ledgerHash).Append('\n')
            .Append("reckonry_version=").Append(reckonryVersion).Append('\n')
            .Append("git_commit=").Append(gitCommit).Append('\n')
            .Append("generated_utc=").Append(generatedAtUtc.ToString("O"));

        return builder.ToString();
    }

    public static DossierStatusKind ResolveStatusKind(
        int validationErrorCount,
        int warningCount,
        bool applies = true)
    {
        if (!applies)
        {
            return DossierStatusKind.NotApplicable;
        }

        if (validationErrorCount > 0)
        {
            return DossierStatusKind.Error;
        }

        return warningCount > 0 ? DossierStatusKind.Warning : DossierStatusKind.Pass;
    }

    private static string BuildQrSvg(string payload)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new SvgQRCode(qrData);
        return qrCode.GetGraphic(4);
    }

    private static bool TryGetDecimal(JsonElement element, string propertyName, out decimal value)
    {
        value = 0m;
        return element.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.Number
            && property.TryGetDecimal(out value);
    }

    private static string MonthLabel(int month)
    {
        return month switch
        {
            1 => "Jan",
            2 => "Feb",
            3 => "Mar",
            4 => "Apr",
            5 => "May",
            6 => "Jun",
            7 => "Jul",
            8 => "Aug",
            9 => "Sep",
            10 => "Oct",
            11 => "Nov",
            12 => "Dec",
            _ => month.ToString()
        };
    }

    private static string[] DefaultChecklist(ITextLocalizer localizer)
    {
        return
        [
            localizer.Text("Checklist.OwnershipTitle"),
            localizer.Text("Checklist.OwnershipPercentage"),
            localizer.Text("Checklist.ForeignStateHandling"),
            localizer.Text("Checklist.BinanceOfficialValues"),
            localizer.Text("Checklist.CreditsF24Advances"),
            localizer.Text("Checklist.RtRequired")
        ];
    }

    private static string LocalizeChecklistItem(ITextLocalizer localizer, string item)
    {
        var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Confirm ownership title"] = "Checklist.OwnershipTitle",
            ["Confirm ownership percentage"] = "Checklist.OwnershipPercentage",
            ["Confirm foreign state blank/handling"] = "Checklist.ForeignStateHandling",
            ["Confirm use of Binance official report values"] = "Checklist.BinanceOfficialValues",
            ["Confirm prior credits/F24/acconti"] = "Checklist.CreditsF24Advances",
            ["Confirm whether RT is required separately"] = "Checklist.RtRequired"
        };

        return mappings.TryGetValue(item, out var key) ? localizer.Text(key) : item;
    }

    private static string GetString(JsonElement element, string propertyName, string fallback)
    {
        return element.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.String
                ? property.GetString() ?? fallback
                : fallback;
    }

    private static int GetInt(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.Number
                ? property.GetInt32()
                : 0;
    }

    private static bool GetBool(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.True;
    }

    public enum DossierStatusKind
    {
        Pass,
        Warning,
        Error,
        NotApplicable
    }

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
