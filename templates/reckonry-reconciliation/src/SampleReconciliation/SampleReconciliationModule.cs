using System.Text.Json;
using Reckonry.Reconciliation.Abstractions;

namespace SampleReconciliation;

public sealed class ExampleReconciliationModule : IReconciliationModule
{
    public ReconciliationModuleDescriptor Descriptor { get; } = new(
        "sample-reconciliation",
        "Sample Provider Reconciliation",
        ReconciliationScope.Provider,
        ProviderId: "sample-provider",
        CountryCode: null,
        ProfessionalReviewRequired: false,
        SupportedInputFormats: ["csv"],
        GeneratedArtifacts: ["reconciliation-summary.json", "reconciliation-summary.md"]);

    public async Task<ReconciliationRunResult> ReconcileAsync(
        ReconciliationRunRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Directory.CreateDirectory(request.OutputFolder);

        var officialCount = CountDataRows(request.OfficialReportsFolder);
        var reckonryCount = CountDataRows(request.ReckonryReportsFolder);
        var status = officialCount == reckonryCount ? "MatchedForReview" : "NeedsManualReview";
        var summary = new ExampleReconciliationSummary(officialCount, reckonryCount, status);

        var jsonPath = Path.Combine(request.OutputFolder, "reconciliation-summary.json");
        var markdownPath = Path.Combine(request.OutputFolder, "reconciliation-summary.md");

        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
        await File.WriteAllTextAsync(markdownPath, BuildMarkdown(summary), cancellationToken);

        return new ReconciliationRunResult(
            Descriptor.Id,
            request.OutputFolder,
            ["reconciliation-summary.json", "reconciliation-summary.md"],
            summary);
    }

    private static int CountDataRows(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return 0;
        }

        return Directory
            .EnumerateFiles(folder, "*.csv")
            .SelectMany(File.ReadLines)
            .Where(row => !string.IsNullOrWhiteSpace(row))
            .Skip(1)
            .Count();
    }

    private static string BuildMarkdown(ExampleReconciliationSummary summary)
    {
        return $"""
        # Sample Reconciliation Summary

        This template summary compares row counts only. It excludes balances,
        values, identifiers, and raw private records.

        Official rows: {summary.OfficialRowCount}
        Reckonry rows: {summary.ReckonryRowCount}
        Status: {summary.Status}
        """;
    }
}

public sealed record ExampleReconciliationSummary(
    int OfficialRowCount,
    int ReckonryRowCount,
    string Status);
