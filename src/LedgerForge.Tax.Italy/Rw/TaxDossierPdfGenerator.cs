using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;

namespace LedgerForge.Tax.Italy.Rw;

public sealed class TaxDossierPdfGenerator : ITaxDossierPdfGenerator
{
    private const string OutputFileNameFormat = "LedgerForge-Tax-Dossier-{0}.pdf";
    private const string AccentColor = "F59E0B";
    private const string DarkColor = "111827";
    private const string GreenColor = "16A34A";
    private const string RedColor = "DC2626";
    private const string YellowColor = "F59E0B";
    private const string GrayColor = "6B7280";
    private const string LightBorderColor = "E5E7EB";

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
        var version = string.IsNullOrWhiteSpace(request.LedgerForgeVersion)
            ? Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown"
            : request.LedgerForgeVersion;
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
            page.DefaultTextStyle(TextStyle.Default.FontFamily("Helvetica").FontSize(10).FontColor(Colors.Black));
            page.PageColor(Colors.White);

            page.Content().Column(pageColumn =>
            {
                pageColumn.Item().Height(420).Background(Color.FromHex(DarkColor)).Padding(46).Column(column =>
                {
                    column.Spacing(22);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Height(76).Element(element => ComposeLogo(element, dossier, darkBackground: true));
                        row.ConstantItem(112).AlignRight().Element(element => ComposeQrCode(element, dossier));
                    });

                    column.Item().PaddingTop(30).Text(dossier.Title)
                        .FontSize(32)
                        .SemiBold()
                        .FontColor(Colors.White);

                    column.Item().Text(dossier.Localizer.Text("Dossier.PreparedForReview"))
                        .FontSize(16)
                        .FontColor(Colors.Grey.Lighten2);

                    column.Item().Row(row =>
                    {
                        row.AutoItem().Element(element => ComposeStatusBadge(element, dossier.ReadinessStatus, DossierStatusKind.Error));
                        row.ConstantItem(18);
                        row.AutoItem().Text(dossier.Localizer.Format("Dossier.Subtitle", dossier.Year))
                            .FontSize(11)
                            .FontColor(Colors.Grey.Lighten1);
                    });

                    column.Item().Text(dossier.Localizer.Text("Dossier.CoverBody"))
                        .FontSize(10)
                        .FontColor(Colors.Grey.Lighten2)
                        .LineHeight(1.35f);
                });

                pageColumn.Item().Padding(46).Column(column =>
                {
                    column.Spacing(22);
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Element(element => ComposeCoverMetric(element, dossier.Localizer.Text("Label.GeneratedUtc"), dossier.GeneratedAtUtc.ToString("O")));
                        row.RelativeItem().Element(element => ComposeCoverMetric(element, dossier.Localizer.Text("Label.ShortLedgerHash"), dossier.ShortLedgerHash));
                    });

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Element(element => ComposeCoverMetric(element, dossier.Localizer.Text("Label.LedgerForgeVersion"), dossier.LedgerForgeVersion));
                        row.RelativeItem().Element(element => ComposeCoverMetric(element, dossier.Localizer.Text("Label.GitCommit"), dossier.GitCommit));
                    });

                    column.Item().PaddingTop(30).AlignBottom().Text(dossier.Localizer.Text("Footer.GeneratedBy"))
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken2);
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
            page.Margin(38);
            page.DefaultTextStyle(TextStyle.Default.FontFamily("Helvetica").FontSize(9).FontColor(Colors.Black));
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
                column.Item().Text(dossier.Title).FontSize(9).SemiBold();
                column.Item().Text(sectionTitle).FontSize(8).FontColor(Colors.Grey.Darken2);
            });
            row.ConstantItem(90).AlignRight().Text(dossier.Localizer.Text("Header.ReviewOnly")).FontSize(8).FontColor(Color.FromHex(AccentColor));
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
                    .FontColor(Colors.Grey.Darken2);
            });
            row.ConstantItem(140).Text(dossier.Localizer.Text("Footer.NotTaxFiling"))
                .FontSize(8)
                .FontColor(Colors.Grey.Darken2);
            row.ConstantItem(80).AlignRight().Text(text =>
            {
                text.Span(dossier.Localizer.Text("Footer.Page")).FontSize(8).FontColor(Colors.Grey.Darken2);
                text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Darken2);
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
            column.Spacing(8);
            column.Item().Text(dossier.Localizer.Text("Section.TableOfContents")).FontSize(18).SemiBold();
            foreach (var section in sections)
            {
                column.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5).Row(row =>
                {
                    row.RelativeItem().SectionLink(section.Id).Text(section.Title).FontSize(11).FontColor(Colors.Blue.Darken2);
                    row.ConstantItem(24).AlignRight().Text("•").FontColor(Color.FromHex(AccentColor));
                });
            }
        });
    }

    private static void ComposeExecutiveSummary(IContainer container, TaxDossierViewModel dossier)
    {
        container.Column(column =>
        {
            column.Spacing(14);
            column.Item().Row(row =>
            {
                row.RelativeItem().Element(element => ComposeSummaryStatus(
                    element,
                    dossier.Localizer.Text("Label.Readiness"),
                    dossier.ReadinessStatus,
                    DossierStatusKind.Error));
                row.RelativeItem().Element(element => ComposeSummaryStatus(
                    element,
                    dossier.Localizer.Text("Section.LedgerIntegrity"),
                    dossier.UnknownEventCount == 0 ? dossier.Localizer.Text("Value.Pass") : dossier.Localizer.Text("Value.Warning"),
                    dossier.UnknownEventCount == 0 ? DossierStatusKind.Pass : DossierStatusKind.Warning));
                row.RelativeItem().Element(element => ComposeSummaryStatus(
                    element,
                    dossier.Localizer.Text("Section.BinanceReconciliation"),
                    dossier.Reconciliation.OfficialReportsAvailable ? dossier.Localizer.Text("Value.Pass") : dossier.Localizer.Text("Value.NotApplicable"),
                    dossier.Reconciliation.OfficialReportsAvailable ? DossierStatusKind.Pass : DossierStatusKind.NotApplicable));
            });
            column.Item().Text(dossier.Localizer.Text("Text.ExecutiveSummary"))
                .FontSize(11)
                .LineHeight(1.35f);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                AddKeyValue(table, dossier.Localizer.Text("Label.ImportedRows"), dossier.ImportedRowCount.ToString());
                AddKeyValue(table, dossier.Localizer.Text("Label.LedgerEvents"), dossier.LedgerEventCount.ToString());
                AddKeyValue(table, dossier.Localizer.Text("Label.UnknownEvents"), dossier.UnknownEventCount.ToString());
                AddKeyValue(table, dossier.Localizer.Text("Label.OfficialReports"), dossier.OfficialReportDocumentCount.ToString());
                AddKeyValue(table, dossier.Localizer.Text("Label.AssetsDetected"), dossier.AssetsDetectedCount.ToString());
                AddKeyValue(table, dossier.Localizer.Text("Label.MissingValuationEvidence"), dossier.MissingValuationEvidenceCount.ToString());
                AddKeyValue(table, dossier.Localizer.Text("Label.ValidationErrors"), dossier.ValidationErrors.Count.ToString());
                AddKeyValue(table, dossier.Localizer.Text("Label.Warnings"), dossier.Warnings.Count.ToString());
            });
        });
    }

    private static void ComposeLedgerIntegrity(IContainer container, TaxDossierViewModel dossier)
    {
        container.Column(column =>
        {
            column.Spacing(10);
            column.Item().Text(dossier.Localizer.Text("Section.LedgerIntegrity")).FontSize(18).SemiBold();
            column.Item().Text(dossier.Localizer.Text("Text.LedgerIntegrity"))
                .LineHeight(1.3f);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                AddKeyValue(table, dossier.Localizer.Text("Label.LedgerSha256"), dossier.LedgerHashSha256);
                AddKeyValue(table, dossier.Localizer.Text("Label.ImportedRows"), dossier.ImportedRowCount.ToString());
                AddKeyValue(table, dossier.Localizer.Text("Label.LedgerEvents"), dossier.LedgerEventCount.ToString());
                AddKeyValue(table, dossier.Localizer.Text("Label.UnknownEvents"), dossier.UnknownEventCount.ToString());
            });
        });
    }

    private static void ComposeBinanceReconciliation(IContainer container, TaxDossierViewModel dossier)
    {
        container.Column(column =>
        {
            column.Spacing(10);
            column.Item().Text(dossier.Localizer.Text("Section.BinanceReconciliation")).FontSize(18).SemiBold();
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
            column.Item().Text(dossier.Localizer.Text("Section.SourceDocuments")).FontSize(18).SemiBold();
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
            column.Item().Text(dossier.Localizer.Text("Section.PortfolioComposition")).FontSize(18).SemiBold();
            column.Item().Text(dossier.Localizer.Text("Text.PortfolioComposition")).LineHeight(1.3f);

            if (dossier.PortfolioComposition.Count == 0)
            {
                column.Item().Element(element => ComposeEmptyState(
                    element,
                    dossier.Localizer.Text("Label.PortfolioChart"),
                    dossier.Localizer.Text("Text.PortfolioEmpty")));
                return;
            }

            column.Item().Element(element => ComposePortfolioBars(element, dossier));
        });
    }

    private static void ComposeMovementTimeline(IContainer container, TaxDossierViewModel dossier)
    {
        container.Column(column =>
        {
            column.Spacing(12);
            column.Item().Text(dossier.Localizer.Text("Section.MovementTimeline")).FontSize(18).SemiBold();
            column.Item().Text(dossier.Localizer.Text("Text.MovementTimeline")).LineHeight(1.3f);
            column.Item().Height(240).Element(element => ComposeTimelineChart(element, dossier));
        });
    }

    private static void ComposePortfolioBars(IContainer container, TaxDossierViewModel dossier)
    {
        var total = dossier.PortfolioComposition.Sum(asset => asset.Value);
        var palette = new[] { AccentColor, "2563EB", "7C3AED", "059669", "64748B", "DB2777" };

        container.Border(1).BorderColor(Color.FromHex(LightBorderColor)).Padding(16).Column(column =>
        {
            column.Spacing(12);
            for (var i = 0; i < dossier.PortfolioComposition.Count; i++)
            {
                var asset = dossier.PortfolioComposition[i];
                var percent = total <= 0 ? 0 : Math.Max(1, Math.Round(asset.Value / total * 100m, 2));
                var remainder = Math.Max(0.01m, 100m - percent);
                var color = palette[i % palette.Length];

                column.Item().Column(assetColumn =>
                {
                    assetColumn.Spacing(4);
                    assetColumn.Item().Row(row =>
                    {
                        row.RelativeItem().Text(asset.AssetSymbol).SemiBold();
                        row.AutoItem().Text($"{percent:0.##}%").FontColor(Colors.Grey.Darken2);
                    });
                    assetColumn.Item().Height(10).Row(row =>
                    {
                        row.RelativeItem((float)percent).Background(Color.FromHex(color));
                        row.RelativeItem((float)remainder).Background(Colors.Grey.Lighten3);
                    });
                });
            }
        });
    }

    private static void ComposeTimelineChart(IContainer container, TaxDossierViewModel dossier)
    {
        var max = Math.Max(1, dossier.MovementTimeline.Max(month => month.EventCount));

        container.Border(1).BorderColor(Color.FromHex(LightBorderColor)).Padding(16).Row(row =>
        {
            row.Spacing(6);
            foreach (var month in dossier.MovementTimeline)
            {
                var barHeight = Math.Max(4, (int)Math.Round(160m * month.EventCount / max));
                row.RelativeItem().AlignBottom().Column(column =>
                {
                    column.Item().Height(176 - barHeight);
                    column.Item().Height(barHeight).Background(Color.FromHex(AccentColor));
                    column.Item().PaddingTop(4).AlignCenter().Text(month.Label).FontSize(7).FontColor(Colors.Grey.Darken2);
                    column.Item().AlignCenter().Text(month.EventCount.ToString()).FontSize(7).SemiBold();
                });
            }
        });
    }

    private static void ComposeRwDraft(IContainer container, TaxDossierViewModel dossier)
    {
        container.Column(column =>
        {
            column.Spacing(10);
            column.Item().Text(dossier.Localizer.Text("Section.RwDraft")).FontSize(18).SemiBold();
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
            column.Item().Text(dossier.Localizer.Text("Section.Rw8Draft")).FontSize(18).SemiBold();
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
            column.Item().Text(dossier.Localizer.Text("Section.ValidationMissingInputs")).FontSize(18).SemiBold();
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
            column.Item().Text(dossier.Localizer.Text("Section.AccountantChecklist")).FontSize(18).SemiBold();
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
            column.Item().Text(dossier.Localizer.Text("Section.TechnicalAppendix")).FontSize(18).SemiBold();
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn(2);
                });

                AddKeyValue(table, dossier.Localizer.Text("Label.LedgerSha256"), dossier.LedgerHashSha256);
                AddKeyValue(table, dossier.Localizer.Text("Label.ReportSha256"), dossier.Localizer.Text("Value.NotAvailable"));
                AddKeyValue(table, dossier.Localizer.Text("Label.LedgerForgeVersion"), dossier.LedgerForgeVersion);
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
            container.AlignLeft().Width(220).Svg(dossier.LogoSvg);
            return;
        }

        container.Text("LedgerForge")
            .FontSize(22)
            .SemiBold()
            .FontColor(darkBackground ? Colors.White : Color.FromHex(AccentColor));
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
        container.Background(Colors.White).Padding(8).Svg(dossier.QrSvg);
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

    private static void AddKeyValue(TableDescriptor table, string key, string value)
    {
        AddCell(table, key, true);
        AddCell(table, value);
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
        string ledgerForgeVersion,
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
            .Append("ledgerforge_version=").Append(ledgerForgeVersion).Append('\n')
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
        string LedgerForgeVersion,
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
