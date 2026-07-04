using Reckonry.Reconciliation.Abstractions;

namespace SampleReconciliation.Tests;

public sealed class ExampleReconciliationTests
{
    [Fact]
    public void Descriptor_AdvertisesProviderReconciliation()
    {
        var module = new ExampleReconciliationModule();

        Assert.Equal("sample-reconciliation", module.Descriptor.Id);
        Assert.Equal(ReconciliationScope.Provider, module.Descriptor.Scope);
        Assert.Contains("csv", module.Descriptor.SupportedInputFormats);
    }

    [Fact]
    public async Task ReconcileAsync_WritesJsonAndMarkdown()
    {
        var output = Directory.CreateTempSubdirectory("reckonry-template-reconciliation-");
        try
        {
            var official = Path.Combine(AppContext.BaseDirectory, "samples", "official");
            var reckonry = Path.Combine(AppContext.BaseDirectory, "samples", "reckonry");
            var module = new ExampleReconciliationModule();

            var result = await module.ReconcileAsync(new ReconciliationRunRequest(official, reckonry, output.FullName));

            var summary = Assert.IsType<ExampleReconciliationSummary>(result.Summary);
            Assert.Equal("MatchedForReview", summary.Status);
            Assert.True(File.Exists(Path.Combine(output.FullName, "reconciliation-summary.json")));
            Assert.True(File.Exists(Path.Combine(output.FullName, "reconciliation-summary.md")));
        }
        finally
        {
            output.Delete(recursive: true);
        }
    }
}
