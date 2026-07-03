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
}
