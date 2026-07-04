using Reckonry.Core;

namespace SampleImporter.Tests;

public sealed class ExampleSourceImporterTests
{
    [Fact]
    public void Descriptor_AdvertisesImporter()
    {
        var importer = new ExampleSourceImporter();

        Assert.Equal("sample-importer", importer.Descriptor.Id);
        Assert.Contains("Unknown row preservation", importer.Descriptor.SupportedOperations);
    }

    [Fact]
    public void ImportFolder_ParsesKnownRowsAndPreservesUnknownRows()
    {
        var importer = new ExampleSourceImporter();
        var inputFolder = Path.Combine(AppContext.BaseDirectory, "samples");

        var events = importer.ImportFolder(inputFolder);

        Assert.Equal(3, events.Count);
        Assert.Contains(events, item => item.EventType == LedgerEventType.Deposit);
        Assert.Contains(events, item => item.EventType == LedgerEventType.Withdrawal);

        var unknown = Assert.Single(events, item => item.EventType == LedgerEventType.Unknown);
        Assert.Equal("fake-transactions.csv", unknown.SourceReference.SourceFile);
        Assert.Contains("unsupported", unknown.SourceReference.RawData);
    }
}
