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
}
