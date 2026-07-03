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
}
