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
}
