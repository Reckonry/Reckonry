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
    public enum DossierStatusKind
    {
        Pass,
        Warning,
        Error,
        NotApplicable
    }

    private static string BuildQrSvg(string payload)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new SvgQRCode(qrData);
        return qrCode.GetGraphic(4);
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
}
