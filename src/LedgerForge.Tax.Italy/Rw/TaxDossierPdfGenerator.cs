using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LedgerForge.Tax.Italy.Rw;

public sealed class TaxDossierPdfGenerator : ITaxDossierPdfGenerator
{
    private const string OutputFileNameFormat = "LedgerForge-Tax-Dossier-{0}.pdf";
    private const string AccentColor = "F59E0B";

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
            ? checklistElement.EnumerateArray().Select(item => GetString(item, "item", "Review item")).ToArray()
            : DefaultChecklist();

        var gitCommit = string.IsNullOrWhiteSpace(request.GitCommit) ? "Unknown" : request.GitCommit;
        var version = string.IsNullOrWhiteSpace(request.LedgerForgeVersion)
            ? Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown"
            : request.LedgerForgeVersion;

        return new TaxDossierViewModel(
            request.Year,
            DateTimeOffset.UtcNow,
            "NOT READY FOR FILING",
            await ComputeSha256Async(request.LedgerJsonPath, cancellationToken),
            gitCommit,
            version,
            request.RepositoryUrl,
            await ReadLogoAsync(request.LogoSvgPath, cancellationToken),
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
            checklist);
    }

    private static void ComposeDocument(IDocumentContainer container, TaxDossierViewModel dossier)
    {
        ComposeCoverPage(container, dossier);
        ComposeContentPage(container, dossier, "Table of Contents", ComposeTableOfContents);
        ComposeContentPage(container, dossier, "Executive Summary", ComposeExecutiveSummary);
        ComposeContentPage(container, dossier, "Ledger Integrity", ComposeLedgerIntegrity);
        ComposeContentPage(container, dossier, "Binance Reconciliation", ComposeBinanceReconciliation);
        ComposeContentPage(container, dossier, "Source Documents", ComposeSourceDocuments);
        ComposeContentPage(container, dossier, "RW Draft", ComposeRwDraft);
        ComposeContentPage(container, dossier, "RW8 Draft", ComposeRw8Draft);
        ComposeContentPage(container, dossier, "Validation And Missing Inputs", ComposeValidationAndMissingInputs);
        ComposeContentPage(container, dossier, "Accountant Checklist", ComposeAccountantChecklist);
    }

    private static void ComposeCoverPage(IDocumentContainer container, TaxDossierViewModel dossier)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(46);
            page.DefaultTextStyle(TextStyle.Default.FontFamily("Helvetica").FontSize(10).FontColor(Colors.Black));
            page.PageColor(Colors.White);

            page.Content().Column(column =>
            {
                column.Spacing(28);

                column.Item().Height(100).Element(element => ComposeLogo(element, dossier));

                column.Item().Text("LedgerForge Tax Dossier")
                    .FontSize(30)
                    .SemiBold()
                    .FontColor(Colors.Black);

                column.Item().Text($"Review dossier for tax year {dossier.Year}")
                    .FontSize(15)
                    .FontColor(Colors.Grey.Darken2);

                column.Item().Element(element => ComposeStatusBanner(element, dossier.ReadinessStatus));

                column.Item().Text("Generated for accountant, auditor, and tax professional review. This document is not a tax filing and does not provide tax, legal, accounting, or financial advice.")
                    .FontSize(11)
                    .LineHeight(1.35f);

                column.Item().PaddingTop(20).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    AddKeyValue(table, "Ledger SHA256", dossier.LedgerHashSha256);
                    AddKeyValue(table, "Git Commit", dossier.GitCommit);
                    AddKeyValue(table, "LedgerForge Version", dossier.LedgerForgeVersion);
                    AddKeyValue(table, "Generated UTC", dossier.GeneratedAtUtc.ToString("O"));
                });
            });
        });
    }

    private static void ComposeContentPage(
        IDocumentContainer container,
        TaxDossierViewModel dossier,
        string title,
        Action<IContainer, TaxDossierViewModel> content)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(38);
            page.DefaultTextStyle(TextStyle.Default.FontFamily("Helvetica").FontSize(9).FontColor(Colors.Black));
            page.PageColor(Colors.White);

            page.Header().Element(element => ComposeHeader(element, title));
            page.Content().PaddingVertical(18).Element(element => content(element, dossier));
            page.Footer().Element(ComposeFooter);
        });
    }

    private static void ComposeHeader(IContainer container, string sectionTitle)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("LedgerForge Tax Dossier").FontSize(9).SemiBold();
                column.Item().Text(sectionTitle).FontSize(8).FontColor(Colors.Grey.Darken2);
            });
            row.ConstantItem(90).AlignRight().Text("Review only").FontSize(8).FontColor(Color.FromHex(AccentColor));
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text("Not a tax filing. Accountant review required.")
                .FontSize(8)
                .FontColor(Colors.Grey.Darken2);
            row.ConstantItem(80).AlignRight().Text(text =>
            {
                text.Span("Page ").FontSize(8).FontColor(Colors.Grey.Darken2);
                text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Darken2);
            });
        });
    }

    private static void ComposeTableOfContents(IContainer container, TaxDossierViewModel dossier)
    {
        var sections = new[]
        {
            "Executive Summary",
            "Ledger Integrity",
            "Binance Reconciliation",
            "Source Documents",
            "RW Draft",
            "RW8 Draft",
            "Validation And Missing Inputs",
            "Accountant Checklist"
        };

        container.Column(column =>
        {
            column.Spacing(8);
            column.Item().Text("Table of Contents").FontSize(18).SemiBold();
            foreach (var section in sections)
            {
                column.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5).Row(row =>
                {
                    row.RelativeItem().Text(section).FontSize(11);
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
            column.Item().Element(element => ComposeStatusBanner(element, dossier.ReadinessStatus));
            column.Item().Text("This dossier summarizes LedgerForge import, reconciliation, RW draft readiness, and missing inputs for professional review. It is not an autonomous filing output.")
                .FontSize(11)
                .LineHeight(1.35f);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                AddKeyValue(table, "Imported Rows", dossier.ImportedRowCount.ToString());
                AddKeyValue(table, "Ledger Events", dossier.LedgerEventCount.ToString());
                AddKeyValue(table, "Unknown Events", dossier.UnknownEventCount.ToString());
                AddKeyValue(table, "Official Reports", dossier.OfficialReportDocumentCount.ToString());
                AddKeyValue(table, "Assets Detected", dossier.AssetsDetectedCount.ToString());
                AddKeyValue(table, "Missing Valuation Evidence", dossier.MissingValuationEvidenceCount.ToString());
                AddKeyValue(table, "Validation Errors", dossier.ValidationErrors.Count.ToString());
                AddKeyValue(table, "Warnings", dossier.Warnings.Count.ToString());
            });
        });
    }

    private static void ComposeLedgerIntegrity(IContainer container, TaxDossierViewModel dossier)
    {
        container.Column(column =>
        {
            column.Spacing(10);
            column.Item().Text("Ledger Integrity").FontSize(18).SemiBold();
            column.Item().Text("LedgerForge imported the Binance transaction data represented in the local ledger. Unknown event count is reported below for review.")
                .LineHeight(1.3f);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                AddKeyValue(table, "Ledger SHA256", dossier.LedgerHashSha256);
                AddKeyValue(table, "Imported Rows", dossier.ImportedRowCount.ToString());
                AddKeyValue(table, "Ledger Events", dossier.LedgerEventCount.ToString());
                AddKeyValue(table, "Unknown Events", dossier.UnknownEventCount.ToString());
            });
        });
    }

    private static void ComposeBinanceReconciliation(IContainer container, TaxDossierViewModel dossier)
    {
        container.Column(column =>
        {
            column.Spacing(10);
            column.Item().Text("Binance Reconciliation").FontSize(18).SemiBold();
            column.Item().Text("Official Binance tax reports are included as reconciliation evidence when extraction metadata is available. Per-asset RW valuation values were not extracted automatically.")
                .LineHeight(1.3f);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                AddKeyValue(table, "Status", dossier.Reconciliation.Status);
                AddKeyValue(table, "Official Reports Available", dossier.Reconciliation.OfficialReportsAvailable ? "Yes" : "No");
                AddKeyValue(table, "Official Report Documents", dossier.OfficialReportDocumentCount.ToString());
                AddKeyValue(table, "Report Types", string.Join(", ", dossier.Reconciliation.ReportTypes));
            });
        });
    }

    private static void ComposeSourceDocuments(IContainer container, TaxDossierViewModel dossier)
    {
        container.Column(column =>
        {
            column.Spacing(10);
            column.Item().Text("Source Documents").FontSize(18).SemiBold();
            column.Item().Text("Source file names and aggregate import counts are included for traceability. Transaction rows and raw data are not reproduced in this dossier.")
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

                AddHeader(table, "System");
                AddHeader(table, "File");
                AddHeader(table, "Rows");
                AddHeader(table, "Unknown");

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
                column.Item().Text($"Additional source files omitted from this page: {dossier.SourceFiles.Count - 24}.")
                    .FontColor(Colors.Grey.Darken2);
            }
        });
    }

    private static void ComposeRwDraft(IContainer container, TaxDossierViewModel dossier)
    {
        container.Column(column =>
        {
            column.Spacing(10);
            column.Item().Text("RW Draft").FontSize(18).SemiBold();
            column.Item().Text("RW crypto draft lines are generated for professional review. Final RW values must be taken from official Binance reports or reviewed by the accountant.")
                .LineHeight(1.3f);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                AddKeyValue(table, "Assets Detected", dossier.AssetsDetectedCount.ToString());
                AddKeyValue(table, "Filled Valuation Evidence", dossier.FilledValuationEvidenceCount.ToString());
                AddKeyValue(table, "Missing Valuation Evidence", dossier.MissingValuationEvidenceCount.ToString());
                AddKeyValue(table, "Autonomous Filing", "No");
            });
        });
    }

    private static void ComposeRw8Draft(IContainer container, TaxDossierViewModel dossier)
    {
        container.Column(column =>
        {
            column.Spacing(10);
            column.Item().Text("RW8 Draft").FontSize(18).SemiBold();
            column.Item().Text("RW8 remains a draft summary until taxpayer configuration, prior credits, F24 compensations, advances paid, and official valuation evidence are reviewed.")
                .LineHeight(1.3f);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                AddKeyValue(table, "Status", "Draft");
                AddKeyValue(table, "Accountant Review Required", "Yes");
                AddKeyValue(table, "Final Filing Output", "No");
                AddKeyValue(table, "Tax Advice", "No");
            });
        });
    }

    private static void ComposeValidationAndMissingInputs(IContainer container, TaxDossierViewModel dossier)
    {
        container.Column(column =>
        {
            column.Spacing(10);
            column.Item().Text("Validation Errors And Missing Inputs").FontSize(18).SemiBold();
            column.Item().Text("Validation messages are grouped by code and count. Asset-level values are not shown.")
                .LineHeight(1.3f);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                AddHeader(table, "Code");
                AddHeader(table, "Severity");
                AddHeader(table, "Count");

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
            column.Item().Text("Accountant Checklist").FontSize(18).SemiBold();
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

    private static void ComposeLogo(IContainer container, TaxDossierViewModel dossier)
    {
        if (!string.IsNullOrWhiteSpace(dossier.LogoSvg))
        {
            container.AlignLeft().Width(220).Svg(dossier.LogoSvg);
            return;
        }

        container.Text("LedgerForge").FontSize(22).SemiBold().FontColor(Color.FromHex(AccentColor));
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

    private static string[] DefaultChecklist()
    {
        return
        [
            "Confirm ownership title",
            "Confirm ownership percentage",
            "Confirm foreign state blank/handling",
            "Confirm use of Binance official report values",
            "Confirm prior credits/F24/acconti",
            "Confirm whether RT is required separately"
        ];
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

    private sealed record TaxDossierViewModel(
        int Year,
        DateTimeOffset GeneratedAtUtc,
        string ReadinessStatus,
        string LedgerHashSha256,
        string GitCommit,
        string LedgerForgeVersion,
        string? RepositoryUrl,
        string? LogoSvg,
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
        IReadOnlyList<string> AccountantChecklist);

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
