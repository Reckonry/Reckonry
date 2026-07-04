using System.Globalization;
using System.Text;
using Reckonry.Core;
using Reckonry.Reports;

namespace SampleReport;

public sealed class ExampleReportModule : IReportModule
{
    public ReportDescriptor Descriptor { get; } = new(
        "sample-report",
        "Sample Ledger Review",
        ReportScope.Generic,
        CountryCode: null,
        ProviderId: null,
        ProfessionalReviewRequired: false,
        SupportedOutputFormats: ["md"]);
}

public sealed class ExampleReportWriter
{
    public async Task<string> WriteAsync(
        string outputFolder,
        IReadOnlyCollection<LedgerEvent> ledgerEvents,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFolder);
        ArgumentNullException.ThrowIfNull(ledgerEvents);

        Directory.CreateDirectory(outputFolder);
        var path = Path.Combine(outputFolder, "sample-report.md");
        await File.WriteAllTextAsync(path, BuildMarkdown(ledgerEvents), cancellationToken);
        return path;
    }

    private static string BuildMarkdown(IReadOnlyCollection<LedgerEvent> ledgerEvents)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Sample Ledger Review");
        builder.AppendLine();
        builder.AppendLine("Synthetic template report. Not tax, accounting, legal, or financial advice.");
        builder.AppendLine();
        builder.AppendLine($"Ledger events: {ledgerEvents.Count.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine($"Unknown events: {ledgerEvents.Count(item => item.EventType == LedgerEventType.Unknown).ToString(CultureInfo.InvariantCulture)}");
        return builder.ToString();
    }
}
