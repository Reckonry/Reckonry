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
    private static async Task<TaxDossierViewModel> BuildDossierAsync(
        TaxDossierPdfRequest request,
        CancellationToken cancellationToken)
    {
        var localizer = DictionaryTextLocalizer.Create(request.Language, ReportLanguages.Italian);
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
            ? checklistElement.EnumerateArray()
                .Select(item => LocalizeChecklistItem(localizer, GetString(item, "item", "Review item")))
                .ToArray()
            : DefaultChecklist(localizer);

        var gitCommit = string.IsNullOrWhiteSpace(request.GitCommit) ? "Unknown" : request.GitCommit;
        var version = string.IsNullOrWhiteSpace(request.ReckonryVersion)
            ? Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown"
            : request.ReckonryVersion;
        var ledgerHash = await ComputeSha256Async(request.LedgerJsonPath, cancellationToken);
        var generatedAtUtc = DateTimeOffset.UtcNow;
        var movementTimeline = await ReadMovementTimelineAsync(request.LedgerJsonPath, request.Year, cancellationToken);
        var portfolioComposition = ReadPortfolioComposition(report);
        var qrPayload = BuildVerificationQrPayload(
            request.RepositoryUrl,
            ledgerHash,
            version,
            gitCommit,
            generatedAtUtc);

        return new TaxDossierViewModel(
            request.Year,
            generatedAtUtc,
            localizer.Text("Status.NotReadyForFiling"),
            localizer.Text("Dossier.Title"),
            localizer,
            ledgerHash,
            gitCommit,
            version,
            request.RepositoryUrl,
            await ReadLogoAsync(request.LogoSvgPath, cancellationToken),
            BuildQrSvg(qrPayload),
            qrPayload,
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
            checklist,
            portfolioComposition,
            movementTimeline);
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

    private static async Task<IReadOnlyList<MonthlyEventCount>> ReadMovementTimelineAsync(
        string ledgerJsonPath,
        int year,
        CancellationToken cancellationToken)
    {
        using var document = await ReadJsonAsync(ledgerJsonPath, cancellationToken);
        var counts = new int[12];

        var events = document.RootElement.ValueKind == JsonValueKind.Array
            ? document.RootElement
            : document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("events", out var nestedEvents)
                && nestedEvents.ValueKind == JsonValueKind.Array
                    ? nestedEvents
                    : default;

        if (events.ValueKind == JsonValueKind.Array)
        {
            foreach (var ledgerEvent in events.EnumerateArray())
            {
                if (!ledgerEvent.TryGetProperty("timestampUtc", out var timestamp)
                    || timestamp.ValueKind != JsonValueKind.String
                    || !DateTimeOffset.TryParse(timestamp.GetString(), out var parsed)
                    || parsed.UtcDateTime.Year != year)
                {
                    continue;
                }

                counts[parsed.UtcDateTime.Month - 1]++;
            }
        }

        return Enumerable.Range(1, 12)
            .Select(month => new MonthlyEventCount(MonthLabel(month), counts[month - 1]))
            .ToArray();
    }

    private static IReadOnlyList<PortfolioAsset> ReadPortfolioComposition(JsonElement report)
    {
        if (!report.TryGetProperty("cryptoLines", out var lines)
            || lines.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<PortfolioAsset>();
        }

        return lines.EnumerateArray()
            .Select(ReadPortfolioAsset)
            .Where(asset => asset is not null)
            .Select(asset => asset!)
            .OrderByDescending(asset => asset.Value)
            .ThenBy(asset => asset.AssetSymbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static PortfolioAsset? ReadPortfolioAsset(JsonElement line)
    {
        var assetSymbol = GetString(line, "assetSymbol", string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(assetSymbol)
            || !line.TryGetProperty("finalValueEvidence", out var evidence)
            || evidence.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
            || !TryGetDecimal(line, "column8FinalValue", out var finalValue)
            || finalValue <= 0)
        {
            return null;
        }

        return new PortfolioAsset(assetSymbol, finalValue);
    }

    public static string BuildVerificationQrPayload(
        string? repositoryUrl,
        string ledgerHash,
        string reckonryVersion,
        string gitCommit,
        DateTimeOffset generatedAtUtc)
    {
        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(repositoryUrl))
        {
            builder.Append("repository=").Append(repositoryUrl.Trim()).Append('\n');
        }

        builder
            .Append("ledger_sha256=").Append(ledgerHash).Append('\n')
            .Append("reckonry_version=").Append(reckonryVersion).Append('\n')
            .Append("git_commit=").Append(gitCommit).Append('\n')
            .Append("generated_utc=").Append(generatedAtUtc.ToString("O"));

        return builder.ToString();
    }

    public static DossierStatusKind ResolveStatusKind(
        int validationErrorCount,
        int warningCount,
        bool applies = true)
    {
        if (!applies)
        {
            return DossierStatusKind.NotApplicable;
        }

        if (validationErrorCount > 0)
        {
            return DossierStatusKind.Error;
        }

        return warningCount > 0 ? DossierStatusKind.Warning : DossierStatusKind.Pass;
    }

    private static bool TryGetDecimal(JsonElement element, string propertyName, out decimal value)
    {
        value = 0m;
        return element.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.Number
            && property.TryGetDecimal(out value);
    }

    private static string MonthLabel(int month)
    {
        return month switch
        {
            1 => "Jan",
            2 => "Feb",
            3 => "Mar",
            4 => "Apr",
            5 => "May",
            6 => "Jun",
            7 => "Jul",
            8 => "Aug",
            9 => "Sep",
            10 => "Oct",
            11 => "Nov",
            12 => "Dec",
            _ => month.ToString()
        };
    }

    private static string[] DefaultChecklist(ITextLocalizer localizer)
    {
        return
        [
            localizer.Text("Checklist.OwnershipTitle"),
            localizer.Text("Checklist.OwnershipPercentage"),
            localizer.Text("Checklist.ForeignStateHandling"),
            localizer.Text("Checklist.BinanceOfficialValues"),
            localizer.Text("Checklist.CreditsF24Advances"),
            localizer.Text("Checklist.RtRequired")
        ];
    }

    private static string LocalizeChecklistItem(ITextLocalizer localizer, string item)
    {
        var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Confirm ownership title"] = "Checklist.OwnershipTitle",
            ["Confirm ownership percentage"] = "Checklist.OwnershipPercentage",
            ["Confirm foreign state blank/handling"] = "Checklist.ForeignStateHandling",
            ["Confirm use of Binance official report values"] = "Checklist.BinanceOfficialValues",
            ["Confirm prior credits/F24/acconti"] = "Checklist.CreditsF24Advances",
            ["Confirm whether RT is required separately"] = "Checklist.RtRequired"
        };

        return mappings.TryGetValue(item, out var key) ? localizer.Text(key) : item;
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
}
