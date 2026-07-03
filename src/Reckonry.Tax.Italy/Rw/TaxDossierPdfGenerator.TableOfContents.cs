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
}
