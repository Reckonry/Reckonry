using Reckonry.Core;
using Reckonry.Reports;

namespace SampleReport.Tests;

public sealed class ExampleReportTests
{
    [Fact]
    public void Descriptor_AdvertisesReport()
    {
        var module = new ExampleReportModule();

        Assert.Equal("sample-report", module.Descriptor.Id);
        Assert.Equal(ReportScope.Generic, module.Descriptor.Scope);
        Assert.Contains("md", module.Descriptor.SupportedOutputFormats);
    }

    [Fact]
    public async Task Writer_WritesDeterministicMarkdown()
    {
        var output = Directory.CreateTempSubdirectory("reckonry-template-report-");
        try
        {
            var writer = new ExampleReportWriter();
            var events = new[]
            {
                Event(LedgerEventType.Deposit),
                Event(LedgerEventType.Unknown)
            };

            var path = await writer.WriteAsync(output.FullName, events);
            var markdown = await File.ReadAllTextAsync(path);

            Assert.Contains("Ledger events: 2", markdown);
            Assert.Contains("Unknown events: 1", markdown);
        }
        finally
        {
            output.Delete(recursive: true);
        }
    }

    private static LedgerEvent Event(LedgerEventType eventType)
    {
        return new LedgerEvent(
            Guid.NewGuid(),
            new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            eventType,
            $"Synthetic {eventType}",
            new SourceReference("Synthetic", "fake.csv", 2, "fake,row"),
            []);
    }
}
