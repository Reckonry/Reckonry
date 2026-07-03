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
}
