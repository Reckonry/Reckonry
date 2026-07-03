using System.Text.Json;
using LedgerForge.Core;
using LedgerForge.Tax.Italy.Rw;

namespace LedgerForge.Tests;

public sealed class ItalyRwConfigWorkflowTests
{
    [Fact]
    public async Task WriteTemplateAsync_WritesPrivateConfigWithNullPlaceholders()
    {
        var root = Directory.CreateTempSubdirectory("ledgerforge-italy-rw-config-");
        try
        {
            var outputPath = Path.Combine(root.FullName, "italy-rw-2025.json");
            var events = new[]
            {
                CreateEvent("BTC"),
                CreateEvent("ETH")
            };

            var result = await new ItalyRwConfigWorkflow().WriteTemplateAsync(2025, events, outputPath);

            Assert.Equal("italy-rw-2025.json", result.GeneratedFileName);
            Assert.Equal(2, result.TotalAssets);
            Assert.Equal(0, result.FilledValuationCount);
            Assert.Equal(4, result.RemainingMissingValuationCount);
            Assert.Equal(1, result.WarningCount);

            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath));
            var rootElement = document.RootElement;

            Assert.Equal(2025, rootElement.GetProperty("year").GetInt32());
            Assert.Equal(JsonValueKind.Null, rootElement.GetProperty("rw8Inputs").GetProperty("priorCryptoTaxCredit").ValueKind);
            Assert.Equal(JsonValueKind.Null, rootElement.GetProperty("rw8Inputs").GetProperty("cryptoTaxF24Compensations").ValueKind);
            Assert.Equal(JsonValueKind.Null, rootElement.GetProperty("rw8Inputs").GetProperty("cryptoTaxAdvancesPaid").ValueKind);
            Assert.Equal(2, rootElement.GetProperty("assets").GetArrayLength());
            Assert.Equal(JsonValueKind.Null, rootElement.GetProperty("assets")[0].GetProperty("initialValue").GetProperty("valueEur").ValueKind);
            Assert.Equal(JsonValueKind.Null, rootElement.GetProperty("assets")[0].GetProperty("finalValue").GetProperty("valueEur").ValueKind);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task FillFromBinanceAsync_DoesNotInventValuesWhenSummaryHasNoPerAssetValuations()
    {
        var root = Directory.CreateTempSubdirectory("ledgerforge-italy-rw-binance-fill-");
        try
        {
            var workflow = new ItalyRwConfigWorkflow();
            var configPath = Path.Combine(root.FullName, "italy-rw-2025.json");
            await workflow.WriteTemplateAsync(2025, new[] { CreateEvent("BTC") }, configPath);

            var reconciliationPath = Path.Combine(root.FullName, "reconciliation-summary.json");
            await File.WriteAllTextAsync(
                reconciliationPath,
                """
                {
                  "documents": [
                    {
                      "reportType": "ItalyAnnualBalanceReport",
                      "year": 2025,
                      "extractionSucceeded": true,
                      "extractedFieldCount": 4,
                      "status": "MatchedForReview"
                    }
                  ]
                }
                """);

            var outputPath = Path.Combine(root.FullName, "italy-rw-2025.binance-filled.json");
            var result = await workflow.FillFromBinanceAsync(configPath, reconciliationPath, outputPath);

            Assert.Equal("italy-rw-2025.binance-filled.json", result.GeneratedFileName);
            Assert.Equal(1, result.TotalAssets);
            Assert.Equal(0, result.FilledValuationCount);
            Assert.Equal(2, result.RemainingMissingValuationCount);
            Assert.Equal(2, result.WarningCount);

            var json = await File.ReadAllTextAsync(outputPath);
            Assert.Contains("no unambiguous per-asset RW valuation fields", json);
            using var document = JsonDocument.Parse(json);
            Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("assets")[0].GetProperty("initialValue").GetProperty("valueEur").ValueKind);
            Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("assets")[0].GetProperty("finalValue").GetProperty("valueEur").ValueKind);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    private static LedgerEvent CreateEvent(string assetSymbol)
    {
        return new LedgerEvent(
            Guid.NewGuid(),
            new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            LedgerEventType.Deposit,
            $"Fake deposit {assetSymbol}",
            new SourceReference("Fake", "fake.csv", 1, "fake,row"),
            new[] { new LedgerPosting(assetSymbol, 1m, LedgerPostingDirection.In, "Fake:Account") });
    }
}
