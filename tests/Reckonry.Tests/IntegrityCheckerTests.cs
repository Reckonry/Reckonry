using Reckonry.Audit;
using Reckonry.Core;

namespace Reckonry.Tests;

public sealed class IntegrityCheckerTests
{
    [Fact]
    public void Check_DetectsExpectedIntegrityFindings()
    {
        var duplicateA = CreateEvent(
            LedgerEventType.Deposit,
            new SourceReference("Test", "test.csv", 1, "row1"),
            new LedgerPosting("BTC", 1m, LedgerPostingDirection.In, "Test"));
        var duplicateB = duplicateA with
        {
            Id = Guid.NewGuid(),
            SourceReference = new SourceReference("Test", "test.csv", 2, "row2")
        };
        var brokenTransfer = CreateEvent(
            LedgerEventType.Transfer,
            new SourceReference("Test", "test.csv", 3, "row3"),
            new LedgerPosting("ETH", 1m, LedgerPostingDirection.Out, "Test"));
        var missingAsset = CreateEvent(
            LedgerEventType.Withdrawal,
            new SourceReference("Test", "test.csv", 4, "row4"),
            new LedgerPosting("", 1m, LedgerPostingDirection.Out, "Test"));
        var unknown = CreateEvent(
            LedgerEventType.Unknown,
            new SourceReference("Test", "test.csv", 5, "row5"),
            new LedgerPosting("USDT", 1m, LedgerPostingDirection.In, "Test"));
        var missingSource = CreateEvent(
            LedgerEventType.Deposit,
            new SourceReference("", "", 0, ""),
            new LedgerPosting("ADA", 1m, LedgerPostingDirection.In, "Test"));

        var checker = new IntegrityChecker();

        var report = checker.Check(new[] { duplicateA, duplicateB, brokenTransfer, missingAsset, unknown, missingSource });

        Assert.Contains(report.Findings, f => f.Code == "DUPLICATE_TRANSACTIONS");
        Assert.Contains(report.Findings, f => f.Code == "BROKEN_TRANSFERS");
        Assert.Contains(report.Findings, f => f.Code == "MISSING_ASSETS");
        Assert.Contains(report.Findings, f => f.Code == "UNKNOWN_EVENT_RATIO");
        Assert.Contains(report.Findings, f => f.Code == "MISSING_SOURCE_REFERENCES");
        Assert.InRange(report.IntegrityScore, 0, 99);
        Assert.InRange(report.ConfidenceScore, 0, 99);
    }

    [Fact]
    public async Task WriteAsync_WritesIntegrityJsonAndMarkdown()
    {
        var outputFolder = Directory.CreateTempSubdirectory("reckonry-integrity-");
        try
        {
            var checker = new IntegrityChecker();
            var events = new[]
            {
                CreateEvent(
                    LedgerEventType.Deposit,
                    new SourceReference("Test", "test.csv", 1, "row1"),
                    new LedgerPosting("BTC", 1m, LedgerPostingDirection.In, "Test"))
            };

            await checker.WriteAsync(outputFolder.FullName, events);

            var jsonPath = Path.Combine(outputFolder.FullName, "integrity.json");
            var markdownPath = Path.Combine(outputFolder.FullName, "integrity.md");

            Assert.True(File.Exists(jsonPath));
            Assert.True(File.Exists(markdownPath));
            Assert.Contains("\"integrityScore\"", await File.ReadAllTextAsync(jsonPath));
            Assert.Contains("Ledger Integrity Report", await File.ReadAllTextAsync(markdownPath));
        }
        finally
        {
            outputFolder.Delete(recursive: true);
        }
    }

    private static LedgerEvent CreateEvent(
        LedgerEventType eventType,
        SourceReference sourceReference,
        params LedgerPosting[] postings)
    {
        return new LedgerEvent(
            Guid.NewGuid(),
            new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            eventType,
            eventType.ToString(),
            sourceReference,
            postings);
    }
}
