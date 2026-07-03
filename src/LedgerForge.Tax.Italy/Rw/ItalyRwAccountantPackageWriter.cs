using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LedgerForge.Core;

namespace LedgerForge.Tax.Italy.Rw;

public sealed class ItalyRwAccountantPackageWriter(
    IItalyRwReportGenerator? reportGenerator = null)
    : IItalyRwAccountantPackageWriter
{
    private readonly IItalyRwReportGenerator reportGenerator = reportGenerator ?? new ItalyRwReportGenerator();

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<ItalyRwAccountantPackageResult> WriteAsync(
        string ledgerJsonPath,
        string outputFolder,
        int year,
        IReadOnlyCollection<LedgerEvent> ledgerEvents,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ledgerJsonPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFolder);
        ArgumentNullException.ThrowIfNull(ledgerEvents);

        var cryptoAssetSymbols = DetectCandidateCryptoAssetSymbols(ledgerEvents);
        var configuration = new ItalyRwReportConfiguration
        {
            CryptoAssetSymbols = cryptoAssetSymbols
        };

        var report = reportGenerator.GenerateCryptoDraft(
            year,
            ledgerEvents,
            configuration,
            Array.Empty<ItalyRwAssetValuation>());

        var sourceFiles = BuildSourceFiles(ledgerEvents);
        var reconciliation = await ReadReconciliationStatusAsync(outputFolder, year, cancellationToken);
        var validationMessages = report.ValidationMessages
            .Concat(BuildPackageValidationMessages(cryptoAssetSymbols))
            .Concat(BuildReconciliationValidationMessages(reconciliation))
            .ToArray();

        var enrichedReport = report with { ValidationMessages = validationMessages };
        var ledgerHash = await ComputeSha256Async(ledgerJsonPath, cancellationToken);
        var readinessStatus = enrichedReport.CanFinalize ? "READY FOR PROFESSIONAL REVIEW" : "NOT READY FOR FILING";
        var package = new AccountantPackageDocument(
            year,
            DateTimeOffset.UtcNow,
            readinessStatus,
            ledgerHash,
            sourceFiles,
            reconciliation,
            enrichedReport);

        Directory.CreateDirectory(outputFolder);

        var markdownPath = Path.Combine(outputFolder, $"italy-rw-accountant-{year}.md");
        var csvPath = Path.Combine(outputFolder, $"italy-rw-accountant-{year}.csv");
        var jsonPath = Path.Combine(outputFolder, $"italy-rw-accountant-{year}.json");

        await File.WriteAllTextAsync(markdownPath, BuildMarkdown(package), cancellationToken);
        await File.WriteAllTextAsync(csvPath, BuildCsv(enrichedReport.CryptoLines), cancellationToken);
        await using (var jsonStream = File.Create(jsonPath))
        {
            await JsonSerializer.SerializeAsync(jsonStream, package, JsonOptions, cancellationToken);
        }

        return new ItalyRwAccountantPackageResult(
            new[]
            {
                Path.GetFileName(markdownPath),
                Path.GetFileName(csvPath),
                Path.GetFileName(jsonPath)
            },
            readinessStatus,
            validationMessages.Count(IsMissingInput),
            validationMessages.Count(message => message.Severity == RwValidationSeverity.Warning));
    }

    private static IReadOnlyList<RwValidationMessage> BuildPackageValidationMessages(
        IReadOnlyCollection<string> cryptoAssetSymbols)
    {
        if (cryptoAssetSymbols.Count == 0)
        {
            return Array.Empty<RwValidationMessage>();
        }

        return new[]
        {
            new RwValidationMessage(
                RwValidationSeverity.Error,
                "MissingCryptoAssetProfessionalConfirmation",
                "Candidate crypto asset symbols were detected from the ledger and must be confirmed before filing.")
        };
    }

    private static IReadOnlyList<RwValidationMessage> BuildReconciliationValidationMessages(
        ReconciliationReviewSummary reconciliation)
    {
        if (reconciliation.Status == "MatchedForReview")
        {
            return Array.Empty<RwValidationMessage>();
        }

        return new[]
        {
            new RwValidationMessage(
                RwValidationSeverity.Warning,
                "BinanceReconciliationNeedsReview",
                "Binance reconciliation is missing or needs manual review.")
        };
    }

    private static IReadOnlyCollection<string> DetectCandidateCryptoAssetSymbols(
        IReadOnlyCollection<LedgerEvent> ledgerEvents)
    {
        return ledgerEvents
            .SelectMany(ledgerEvent => ledgerEvent.Postings)
            .Select(posting => posting.AssetSymbol)
            .Where(asset => !string.IsNullOrWhiteSpace(asset))
            .Select(asset => asset.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(asset => asset, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<SourceFileSummary> BuildSourceFiles(
        IReadOnlyCollection<LedgerEvent> ledgerEvents)
    {
        return ledgerEvents
            .GroupBy(
                ledgerEvent => new
                {
                    ledgerEvent.SourceReference.SourceSystem,
                    ledgerEvent.SourceReference.SourceFile
                })
            .Select(group => new SourceFileSummary(
                group.Key.SourceSystem,
                group.Key.SourceFile,
                group.Count(),
                group.Count(ledgerEvent => ledgerEvent.EventType == LedgerEventType.Unknown)))
            .OrderBy(summary => summary.SourceSystem, StringComparer.OrdinalIgnoreCase)
            .ThenBy(summary => summary.SourceFile, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static async Task<ReconciliationReviewSummary> ReadReconciliationStatusAsync(
        string outputFolder,
        int year,
        CancellationToken cancellationToken)
    {
        var outputDirectory = new DirectoryInfo(Path.GetFullPath(outputFolder));
        var rootOutput = outputDirectory.Parent?.FullName ?? outputDirectory.FullName;
        var reconciliationPath = Path.Combine(rootOutput, "reconciliation", "reconciliation-summary.json");

        if (!File.Exists(reconciliationPath))
        {
            return new ReconciliationReviewSummary(
                "NotAvailable",
                "No Binance reconciliation summary was found next to the accountant output folder.",
                Array.Empty<ReconciliationDocumentReview>());
        }

        await using var stream = File.OpenRead(reconciliationPath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("documents", out var documents)
            || documents.ValueKind != JsonValueKind.Array)
        {
            return new ReconciliationReviewSummary(
                "NeedsManualReview",
                "Binance reconciliation summary exists but could not be read.",
                Array.Empty<ReconciliationDocumentReview>());
        }

        var documentReviews = new List<ReconciliationDocumentReview>();
        foreach (var item in documents.EnumerateArray())
        {
            var itemYear = GetNullableInt(item, "year");
            if (itemYear != year)
            {
                continue;
            }

            documentReviews.Add(new ReconciliationDocumentReview(
                GetString(item, "reportType", "Unknown"),
                itemYear,
                GetBool(item, "extractionSucceeded"),
                GetInt(item, "extractedFieldCount"),
                GetString(item, "status", "Unknown")));
        }

        var status = documentReviews.Count == 0
            ? "MissingOfficialReport"
            : documentReviews.Any(review => review.Status != "MatchedForReview")
                ? "NeedsManualReview"
                : "MatchedForReview";

        return new ReconciliationReviewSummary(
            status,
            "Binance reconciliation status is included for accountant review only.",
            documentReviews);
    }

    private static async Task<string> ComputeSha256Async(
        string ledgerJsonPath,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(ledgerJsonPath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string BuildMarkdown(AccountantPackageDocument package)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# Italy RW Accountant Draft Report {package.Year}");
        builder.AppendLine();
        builder.AppendLine($"Status: **{package.ReadinessStatus}**");
        builder.AppendLine();
        builder.AppendLine("This package is for professional review only. It is not a final tax filing and is not tax, legal, accounting, or financial advice.");
        builder.AppendLine();
        builder.AppendLine($"Ledger SHA-256: `{package.LedgerHashSha256}`");
        builder.AppendLine();

        AppendValidationSection(builder, "Validation Errors", package.Report.Errors);
        AppendValidationSection(builder, "Warnings", package.Report.Warnings);
        AppendSourceFiles(builder, package.SourceFiles);
        AppendReconciliation(builder, package.Reconciliation);
        AppendValuationEvidence(builder, package.Report.CryptoLines);
        AppendRwLines(builder, package.Report.CryptoLines);
        AppendRw8(builder, package.Report.Rw8);

        return builder.ToString();
    }

    private static void AppendValidationSection(
        StringBuilder builder,
        string title,
        IReadOnlyList<RwValidationMessage> messages)
    {
        builder.AppendLine($"## {title}");
        builder.AppendLine();
        if (messages.Count == 0)
        {
            builder.AppendLine("None.");
            builder.AppendLine();
            return;
        }

        builder.AppendLine("| Code | Asset | Message |");
        builder.AppendLine("| --- | --- | --- |");
        foreach (var message in messages)
        {
            builder
                .Append("| ")
                .Append(EscapeMarkdown(message.Code))
                .Append(" | ")
                .Append(EscapeMarkdown(message.AssetSymbol ?? string.Empty))
                .Append(" | ")
                .Append(EscapeMarkdown(message.Message))
                .AppendLine(" |");
        }

        builder.AppendLine();
    }

    private static void AppendSourceFiles(
        StringBuilder builder,
        IReadOnlyList<SourceFileSummary> sourceFiles)
    {
        builder.AppendLine("## Source Files Summary");
        builder.AppendLine();
        builder.AppendLine("| Source System | Source File | Events | Unknown Events |");
        builder.AppendLine("| --- | --- | ---: | ---: |");
        foreach (var sourceFile in sourceFiles)
        {
            builder
                .Append("| ")
                .Append(EscapeMarkdown(sourceFile.SourceSystem))
                .Append(" | ")
                .Append(EscapeMarkdown(sourceFile.SourceFile))
                .Append(" | ")
                .Append(sourceFile.EventCount)
                .Append(" | ")
                .Append(sourceFile.UnknownEventCount)
                .AppendLine(" |");
        }

        builder.AppendLine();
    }

    private static void AppendReconciliation(
        StringBuilder builder,
        ReconciliationReviewSummary reconciliation)
    {
        builder.AppendLine("## Binance Reconciliation Status");
        builder.AppendLine();
        builder.AppendLine($"Status: **{EscapeMarkdown(reconciliation.Status)}**");
        builder.AppendLine();
        builder.AppendLine(EscapeMarkdown(reconciliation.Notes));
        builder.AppendLine();

        if (reconciliation.Documents.Count == 0)
        {
            builder.AppendLine("No reconciliation documents were available for this year.");
            builder.AppendLine();
            return;
        }

        builder.AppendLine("| Report Type | Year | Extraction | Fields | Status |");
        builder.AppendLine("| --- | ---: | --- | ---: | --- |");
        foreach (var document in reconciliation.Documents)
        {
            builder
                .Append("| ")
                .Append(EscapeMarkdown(document.ReportType))
                .Append(" | ")
                .Append(document.Year?.ToString(CultureInfo.InvariantCulture) ?? "Unknown")
                .Append(" | ")
                .Append(document.ExtractionSucceeded ? "Succeeded" : "Not succeeded")
                .Append(" | ")
                .Append(document.ExtractedFieldCount)
                .Append(" | ")
                .Append(EscapeMarkdown(document.Status))
                .AppendLine(" |");
        }

        builder.AppendLine();
    }

    private static void AppendValuationEvidence(
        StringBuilder builder,
        IReadOnlyList<ItalyRwLine> lines)
    {
        builder.AppendLine("## Valuation Evidence Summary");
        builder.AppendLine();
        builder.AppendLine("| Asset | Initial Evidence | Final Evidence |");
        builder.AppendLine("| --- | --- | --- |");
        foreach (var line in lines)
        {
            builder
                .Append("| ")
                .Append(EscapeMarkdown(line.AssetSymbol))
                .Append(" | ")
                .Append(EscapeMarkdown(DescribeEvidence(line.InitialValueEvidence)))
                .Append(" | ")
                .Append(EscapeMarkdown(DescribeEvidence(line.FinalValueEvidence)))
                .AppendLine(" |");
        }

        builder.AppendLine();
    }

    private static void AppendRwLines(
        StringBuilder builder,
        IReadOnlyList<ItalyRwLine> lines)
    {
        builder.AppendLine("## RW Crypto Lines");
        builder.AppendLine();
        builder.AppendLine("The CSV and JSON files contain the full RW1-RW5 column set for accountant review.");
        builder.AppendLine();
        builder.AppendLine($"Line count: {lines.Count}");
        builder.AppendLine();
    }

    private static void AppendRw8(StringBuilder builder, ItalyRw8Summary rw8)
    {
        builder.AppendLine("## RW8 Summary");
        builder.AppendLine();
        builder.AppendLine("| Column 1 | Column 2 | Column 3 | Column 4 | Column 5 | Column 6 |");
        builder.AppendLine("| ---: | ---: | ---: | ---: | ---: | ---: |");
        builder
            .Append("| ")
            .Append(FormatDecimal(rw8.Column1TotalTaxDue))
            .Append(" | ")
            .Append(FormatDecimal(rw8.Column2PreviousDeclarationExcess))
            .Append(" | ")
            .Append(FormatDecimal(rw8.Column3F24CompensatedExcess))
            .Append(" | ")
            .Append(FormatDecimal(rw8.Column4AdvancesPaid))
            .Append(" | ")
            .Append(FormatDecimal(rw8.Column5TaxDebit))
            .Append(" | ")
            .Append(FormatDecimal(rw8.Column6TaxCredit))
            .AppendLine(" |");
        builder.AppendLine();
    }

    private static string BuildCsv(IReadOnlyList<ItalyRwLine> lines)
    {
        var builder = new StringBuilder();
        builder.AppendLine("AssetSymbol,Column1OwnershipTitle,Column2PossessionType,Column3AssetCode,Column4ForeignStateCode,Column5OwnershipPercentage,Column6ValuationCriterion,Column7InitialValue,Column8FinalValue,Column9MaximumCurrentAccountValueInNonCooperativeCountries,Column10IvafeOrIcHoldingDays,Column11IvieHoldingMonths,Column12ForeignTaxCredit,Column13IvieDeduction,Column14IncomeScheduleCode,Column15ParticipationPercentage,Column16MonitoringOnly,Column17BeneficialOwnerEntityTaxCode,Column18CoOwnerTaxCode,Column19CoOwnerTaxCode,Column20MoreThanTwoCoOwners,Column21PrivilegedTaxRegime,Column29Ivafe,Column30IvafeDue,Column31Ivie,Column32IvieDue,Column33Ic,Column34IcDue");

        foreach (var line in lines)
        {
            builder
                .Append(EscapeCsv(line.AssetSymbol)).Append(',')
                .Append(EscapeCsv(line.Column1OwnershipTitle?.ToString() ?? string.Empty)).Append(',')
                .Append(EscapeCsv(line.Column2PossessionType?.ToString() ?? string.Empty)).Append(',')
                .Append(line.Column3AssetCode).Append(',')
                .Append(EscapeCsv(line.Column4ForeignStateCode ?? string.Empty)).Append(',')
                .Append(FormatNullableDecimal(line.Column5OwnershipPercentage)).Append(',')
                .Append(EscapeCsv(line.Column6ValuationCriterion?.ToString() ?? string.Empty)).Append(',')
                .Append(FormatNullableDecimal(line.Column7InitialValue)).Append(',')
                .Append(FormatNullableDecimal(line.Column8FinalValue)).Append(',')
                .Append(FormatNullableDecimal(line.Column9MaximumCurrentAccountValueInNonCooperativeCountries)).Append(',')
                .Append(line.Column10IvafeOrIcHoldingDays?.ToString(CultureInfo.InvariantCulture) ?? string.Empty).Append(',')
                .Append(line.Column11IvieHoldingMonths?.ToString(CultureInfo.InvariantCulture) ?? string.Empty).Append(',')
                .Append(FormatNullableDecimal(line.Column12ForeignTaxCredit)).Append(',')
                .Append(FormatNullableDecimal(line.Column13IvieDeduction)).Append(',')
                .Append(line.Column14IncomeScheduleCode?.ToString(CultureInfo.InvariantCulture) ?? string.Empty).Append(',')
                .Append(FormatNullableDecimal(line.Column15ParticipationPercentage)).Append(',')
                .Append(line.Column16MonitoringOnly).Append(',')
                .Append(EscapeCsv(line.Column17BeneficialOwnerEntityTaxCode ?? string.Empty)).Append(',')
                .Append(EscapeCsv(line.Column18CoOwnerTaxCode ?? string.Empty)).Append(',')
                .Append(EscapeCsv(line.Column19CoOwnerTaxCode ?? string.Empty)).Append(',')
                .Append(line.Column20MoreThanTwoCoOwners).Append(',')
                .Append(line.Column21PrivilegedTaxRegime).Append(',')
                .Append(FormatNullableDecimal(line.Column29Ivafe)).Append(',')
                .Append(FormatNullableDecimal(line.Column30IvafeDue)).Append(',')
                .Append(FormatNullableDecimal(line.Column31Ivie)).Append(',')
                .Append(FormatNullableDecimal(line.Column32IvieDue)).Append(',')
                .Append(FormatNullableDecimal(line.Column33Ic)).Append(',')
                .Append(FormatNullableDecimal(line.Column34IcDue))
                .AppendLine();
        }

        return builder.ToString();
    }

    private static bool IsMissingInput(RwValidationMessage message)
    {
        return message.Severity == RwValidationSeverity.Error
            && message.Code.StartsWith("Missing", StringComparison.OrdinalIgnoreCase);
    }

    private static string DescribeEvidence(RwValuationEvidence? evidence)
    {
        if (evidence is null)
        {
            return "Missing";
        }

        return $"{evidence.GetType().Name}; {evidence.SourceName}; {evidence.SourceTimestamp:O}; confidence {FormatDecimal(evidence.Confidence)}";
    }

    private static string GetString(JsonElement element, string propertyName, string fallback)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? fallback
            : fallback;
    }

    private static int? GetNullableInt(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Number
            ? property.GetInt32()
            : null;
    }

    private static int GetInt(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Number
            ? property.GetInt32()
            : 0;
    }

    private static bool GetBool(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.True;
    }

    private static string FormatNullableDecimal(decimal? value)
    {
        return value is null ? string.Empty : FormatDecimal(value.Value);
    }

    private static string FormatDecimal(decimal value)
    {
        return value.ToString("0.############################", CultureInfo.InvariantCulture);
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private static string EscapeMarkdown(string value)
    {
        return value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
    }

    public sealed record AccountantPackageDocument(
        int Year,
        DateTimeOffset GeneratedAtUtc,
        string ReadinessStatus,
        string LedgerHashSha256,
        IReadOnlyList<SourceFileSummary> SourceFiles,
        ReconciliationReviewSummary Reconciliation,
        ItalyRwReport Report);

    public sealed record SourceFileSummary(
        string SourceSystem,
        string SourceFile,
        int EventCount,
        int UnknownEventCount);

    public sealed record ReconciliationReviewSummary(
        string Status,
        string Notes,
        IReadOnlyList<ReconciliationDocumentReview> Documents);

    public sealed record ReconciliationDocumentReview(
        string ReportType,
        int? Year,
        bool ExtractionSucceeded,
        int ExtractedFieldCount,
        string Status);
}
