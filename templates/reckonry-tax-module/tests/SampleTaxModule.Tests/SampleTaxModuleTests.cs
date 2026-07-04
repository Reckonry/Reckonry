using Reckonry.Core;
using Reckonry.Tax.Abstractions;

namespace SampleTaxModule.Tests;

public sealed class ExampleTaxModuleTests
{
    [Fact]
    public void Descriptor_AdvertisesCountryMetadata()
    {
        var module = new ExampleTaxModulePlugin();

        Assert.Equal("XX", module.Descriptor.CountryCode);
        Assert.Equal(ProfessionalReviewStatus.Required, module.Descriptor.ProfessionalReviewStatus);
        Assert.NotEmpty(module.Descriptor.OfficialSources);
    }

    [Fact]
    public void Analyze_ReturnsWarningsWithoutMutatingLedger()
    {
        var module = new ExampleTaxModulePlugin();
        var ledgerEvent = new LedgerEvent(
            Guid.NewGuid(),
            new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            LedgerEventType.Unknown,
            "Synthetic unknown event",
            new SourceReference("Synthetic", "fake.csv", 2, "fake,row"),
            []);

        var result = module.Analyze(new TaxReportRequest(2025, [ledgerEvent]));

        Assert.Equal(module.Descriptor, result.Module);
        Assert.Equal(2025, result.Year);
        Assert.Contains(result.Warnings, warning => warning.Contains("does not calculate tax", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Warnings, warning => warning.Contains("Unknown ledger events", StringComparison.OrdinalIgnoreCase));
    }
}
