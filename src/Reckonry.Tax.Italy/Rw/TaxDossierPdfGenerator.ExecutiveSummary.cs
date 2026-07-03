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
}
