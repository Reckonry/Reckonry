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
}
