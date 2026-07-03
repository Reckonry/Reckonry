using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Reckonry.Core;

namespace Reckonry.Audit;

public sealed class IntegrityChecker : IIntegrityChecker
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public LedgerIntegrityReport Check(IReadOnlyCollection<LedgerEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        var orderedEvents = events.OrderBy(e => e.TimestampUtc).ToArray();
        var findings = new List<IntegrityFinding>();
        var totalPostings = orderedEvents.Sum(e => e.Postings.Count);

        AddFindingIfAny(findings, CheckDuplicateTransactions(orderedEvents));
        AddFindingIfAny(findings, CheckBrokenTransfers(orderedEvents));
        AddFindingIfAny(findings, CheckMissingAssets(orderedEvents));
        AddFindingIfAny(findings, CheckNegativeBalances(orderedEvents));
        AddFindingIfAny(findings, CheckUnknownEventRatio(orderedEvents));
        AddFindingIfAny(findings, CheckUnknownPostingRatio(orderedEvents, totalPostings));
        AddFindingIfAny(findings, CheckTimestampAnomalies(orderedEvents));
        AddFindingIfAny(findings, CheckCurrencyAnomalies(orderedEvents));
        AddFindingIfAny(findings, CheckFeeAnomalies(orderedEvents));
        AddFindingIfAny(findings, CheckMissingSourceReferences(orderedEvents));

        var integrityScore = CalculateScore(findings, baseScore: 100);
        var confidenceScore = CalculateConfidenceScore(orderedEvents, totalPostings, findings);

        return new LedgerIntegrityReport(
            DateTimeOffset.UtcNow,
            orderedEvents.Length,
            totalPostings,
            integrityScore,
            confidenceScore,
            findings,
            findings.Where(f => f.Severity != IntegritySeverity.Info).Select(f => f.Message).ToArray(),
            findings.Select(f => f.Recommendation).Distinct().ToArray());
    }

    public async Task<LedgerIntegrityReport> WriteAsync(
        string outputFolder,
        IReadOnlyCollection<LedgerEvent> events,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFolder);

        var report = Check(events);
        Directory.CreateDirectory(outputFolder);

        var jsonPath = Path.Combine(outputFolder, "integrity.json");
        var markdownPath = Path.Combine(outputFolder, "integrity.md");

        await using (var jsonStream = File.Create(jsonPath))
        {
            await JsonSerializer.SerializeAsync(jsonStream, report, JsonOptions, cancellationToken);
        }

        await File.WriteAllTextAsync(markdownPath, BuildMarkdown(report), cancellationToken);
        return report;
    }

    private static IntegrityFinding? CheckDuplicateTransactions(IReadOnlyCollection<LedgerEvent> events)
    {
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        var duplicates = 0;

        foreach (var ledgerEvent in events)
        {
            var fingerprint = BuildEventFingerprint(ledgerEvent);
            if (!seen.TryAdd(fingerprint, 1))
            {
                seen[fingerprint]++;
                duplicates++;
            }
        }

        return duplicates == 0
            ? null
            : new IntegrityFinding(
                "DUPLICATE_TRANSACTIONS",
                IntegritySeverity.Warning,
                "Duplicate transactions",
                "Potential duplicate ledger events were detected.",
                duplicates,
                "Review duplicate candidates and add explicit deduplication rules before relying on downstream reports.");
    }

    private static IntegrityFinding? CheckBrokenTransfers(IReadOnlyCollection<LedgerEvent> events)
    {
        var broken = events.Count(e =>
            e.EventType == LedgerEventType.Transfer
            && (!e.Postings.Any(p => p.Direction == LedgerPostingDirection.In)
                || !e.Postings.Any(p => p.Direction == LedgerPostingDirection.Out)));

        return broken == 0
            ? null
            : new IntegrityFinding(
                "BROKEN_TRANSFERS",
                IntegritySeverity.Error,
                "Broken transfers",
                "Transfer events without both incoming and outgoing postings were detected.",
                broken,
                "Fix importer mapping so transfers preserve both sides of movement.");
    }

    private static IntegrityFinding? CheckMissingAssets(IReadOnlyCollection<LedgerEvent> events)
    {
        var missing = events.Sum(e => e.Postings.Count(p => string.IsNullOrWhiteSpace(p.AssetSymbol)));

        return missing == 0
            ? null
            : new IntegrityFinding(
                "MISSING_ASSETS",
                IntegritySeverity.Error,
                "Missing assets",
                "Postings with missing asset symbols were detected.",
                missing,
                "Fix importer mapping so every posting has an asset symbol or becomes an unknown event.");
    }

    private static IntegrityFinding? CheckNegativeBalances(IReadOnlyCollection<LedgerEvent> events)
    {
        var balances = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var negativeAssets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var ledgerEvent in events.OrderBy(e => e.TimestampUtc))
        {
            foreach (var posting in ledgerEvent.Postings.Where(p => !string.IsNullOrWhiteSpace(p.AssetSymbol)))
            {
                var asset = posting.AssetSymbol.Trim().ToUpperInvariant();
                var signedAmount = posting.Direction == LedgerPostingDirection.In ? posting.Amount : -posting.Amount;
                balances[asset] = balances.GetValueOrDefault(asset) + signedAmount;

                if (balances[asset] < 0)
                {
                    negativeAssets.Add(asset);
                }
            }
        }

        return negativeAssets.Count == 0
            ? null
            : new IntegrityFinding(
                "NEGATIVE_BALANCES",
                IntegritySeverity.Warning,
                "Negative balances",
                "One or more assets went negative while replaying ledger events in timestamp order.",
                negativeAssets.Count,
                "Review missing deposits, transfer pairing, chronological order, and exchange export completeness.");
    }

    private static IntegrityFinding? CheckUnknownEventRatio(IReadOnlyCollection<LedgerEvent> events)
    {
        if (events.Count == 0)
        {
            return null;
        }

        var unknownCount = events.Count(e => e.EventType == LedgerEventType.Unknown);
        var ratio = unknownCount / (decimal)events.Count;

        return ratio <= 0.01m
            ? null
            : new IntegrityFinding(
                "UNKNOWN_EVENT_RATIO",
                ratio > 0.05m ? IntegritySeverity.Error : IntegritySeverity.Warning,
                "Unknown event ratio",
                "Unknown ledger events exceed the configured integrity threshold.",
                unknownCount,
                "Improve importer coverage or review exception reports before generating final reports.");
    }

    private static IntegrityFinding? CheckUnknownPostingRatio(IReadOnlyCollection<LedgerEvent> events, int totalPostings)
    {
        if (totalPostings == 0)
        {
            return null;
        }

        var unknownPostings = events
            .Where(e => e.EventType == LedgerEventType.Unknown)
            .Sum(e => e.Postings.Count);
        var ratio = unknownPostings / (decimal)totalPostings;

        return ratio <= 0.01m
            ? null
            : new IntegrityFinding(
                "UNKNOWN_POSTING_RATIO",
                ratio > 0.05m ? IntegritySeverity.Error : IntegritySeverity.Warning,
                "Unknown posting ratio",
                "Unknown event postings exceed the configured integrity threshold.",
                unknownPostings,
                "Review unknown postings and preserve source references for importer improvements.");
    }

    private static IntegrityFinding? CheckTimestampAnomalies(IReadOnlyCollection<LedgerEvent> events)
    {
        var anomalies = events.Count(e =>
            e.TimestampUtc == default
            || e.TimestampUtc == DateTimeOffset.UnixEpoch
            || e.TimestampUtc.Offset != TimeSpan.Zero
            || e.TimestampUtc.Year is < 2009 or > 2100);

        return anomalies == 0
            ? null
            : new IntegrityFinding(
                "TIMESTAMP_ANOMALIES",
                IntegritySeverity.Warning,
                "Timestamp anomalies",
                "Events with placeholder, non-UTC, or implausible timestamps were detected.",
                anomalies,
                "Normalize importer timestamps to UTC and preserve original source timestamp in source data.");
    }

    private static IntegrityFinding? CheckCurrencyAnomalies(IReadOnlyCollection<LedgerEvent> events)
    {
        var anomalies = events.Sum(e => e.Postings.Count(p =>
            p.Value is not null
            && (string.IsNullOrWhiteSpace(p.Value.CurrencyCode)
                || p.Value.CurrencyCode.Length != 3
                || p.Value.Amount < 0)));

        return anomalies == 0
            ? null
            : new IntegrityFinding(
                "CURRENCY_ANOMALIES",
                IntegritySeverity.Warning,
                "Currency anomalies",
                "Posting values with invalid currency metadata were detected.",
                anomalies,
                "Use ISO-style three-letter currency codes and non-negative decimal values.");
    }

    private static IntegrityFinding? CheckFeeAnomalies(IReadOnlyCollection<LedgerEvent> events)
    {
        var feePostings = events.SelectMany(e => e.Postings).Where(IsFeePosting).ToArray();
        var anomalies = feePostings.Count(p => p.Direction != LedgerPostingDirection.Out || p.Amount <= 0);

        return anomalies == 0
            ? null
            : new IntegrityFinding(
                "FEE_ANOMALIES",
                IntegritySeverity.Warning,
                "Fee anomalies",
                "Fee postings with non-outgoing direction or non-positive amounts were detected.",
                anomalies,
                "Map fees as outgoing positive quantities from an explicit fee account.");
    }

    private static IntegrityFinding? CheckMissingSourceReferences(IReadOnlyCollection<LedgerEvent> events)
    {
        var missing = events.Count(e =>
            e.SourceReference is null
            || string.IsNullOrWhiteSpace(e.SourceReference.SourceSystem)
            || string.IsNullOrWhiteSpace(e.SourceReference.SourceFile)
            || e.SourceReference.SourceRowNumber <= 0
            || string.IsNullOrWhiteSpace(e.SourceReference.RawData));

        return missing == 0
            ? null
            : new IntegrityFinding(
                "MISSING_SOURCE_REFERENCES",
                IntegritySeverity.Error,
                "Missing source references",
                "Events without complete source references were detected.",
                missing,
                "Preserve source system, source file, source row number, and raw data for every imported event.");
    }

    private static bool IsFeePosting(LedgerPosting posting)
    {
        return posting.Account.Contains("fee", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildEventFingerprint(LedgerEvent ledgerEvent)
    {
        var builder = new StringBuilder();
        builder.Append(ledgerEvent.TimestampUtc.ToUnixTimeSeconds()).Append('|');
        builder.Append(ledgerEvent.EventType).Append('|');
        builder.Append(ledgerEvent.Postings.Count).Append('|');

        foreach (var posting in ledgerEvent.Postings.OrderBy(p => p.AssetSymbol).ThenBy(p => p.Direction).ThenBy(p => p.Amount))
        {
            builder
                .Append(posting.AssetSymbol.Trim().ToUpperInvariant())
                .Append(':')
                .Append(posting.Direction)
                .Append(':')
                .Append(posting.Amount.ToString(CultureInfo.InvariantCulture))
                .Append('|');
        }

        return builder.ToString();
    }

    private static int CalculateScore(IReadOnlyCollection<IntegrityFinding> findings, int baseScore)
    {
        var penalty = findings.Sum(f => f.Severity switch
        {
            IntegritySeverity.Error => 15,
            IntegritySeverity.Warning => 7,
            _ => 2
        });

        return Math.Clamp(baseScore - penalty, 0, 100);
    }

    private static int CalculateConfidenceScore(
        IReadOnlyCollection<LedgerEvent> events,
        int totalPostings,
        IReadOnlyCollection<IntegrityFinding> findings)
    {
        var score = CalculateScore(findings, baseScore: 100);

        if (events.Count == 0)
        {
            score -= 30;
        }

        if (totalPostings == 0)
        {
            score -= 20;
        }

        var unknownRatio = events.Count == 0
            ? 1m
            : events.Count(e => e.EventType == LedgerEventType.Unknown) / (decimal)events.Count;
        score -= (int)Math.Round(unknownRatio * 50, MidpointRounding.AwayFromZero);

        return Math.Clamp(score, 0, 100);
    }

    private static string BuildMarkdown(LedgerIntegrityReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Ledger Integrity Report");
        builder.AppendLine();
        builder.AppendLine($"Generated at UTC: `{report.GeneratedAtUtc:O}`");
        builder.AppendLine();
        builder.AppendLine($"Integrity Score: **{report.IntegrityScore}**");
        builder.AppendLine($"Confidence Score: **{report.ConfidenceScore}**");
        builder.AppendLine();
        builder.AppendLine("| Code | Severity | Category | Count | Recommendation |");
        builder.AppendLine("| --- | --- | --- | ---: | --- |");

        foreach (var finding in report.Findings)
        {
            builder
                .Append("| ")
                .Append(finding.Code)
                .Append(" | ")
                .Append(finding.Severity)
                .Append(" | ")
                .Append(finding.Category)
                .Append(" | ")
                .Append(finding.Count)
                .Append(" | ")
                .Append(finding.Recommendation.Replace("|", "\\|", StringComparison.Ordinal))
                .AppendLine(" |");
        }

        if (report.Findings.Count == 0)
        {
            builder.AppendLine("| NONE | Info | No findings | 0 | No action required. |");
        }

        return builder.ToString();
    }

    private static void AddFindingIfAny(ICollection<IntegrityFinding> findings, IntegrityFinding? finding)
    {
        if (finding is not null)
        {
            findings.Add(finding);
        }
    }
}
