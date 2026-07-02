using LedgerForge.Core;
using LedgerForge.Storage;

namespace LedgerForge.Tests;

public sealed class JsonLedgerValidatorTests
{
    [Fact]
    public async Task ValidateFileAsync_ReturnsPassForGeneratedLedger()
    {
        var outputFolder = Directory.CreateTempSubdirectory("ledgerforge-validator-");
        try
        {
            var ledgerPath = Path.Combine(outputFolder.FullName, "ledger.json");
            var store = new JsonLedgerStore();
            await store.WriteAsync(
                ledgerPath,
                new[]
                {
                    CreateEvent()
                });

            var result = await new JsonLedgerValidator().ValidateFileAsync(ledgerPath);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }
        finally
        {
            outputFolder.Delete(recursive: true);
        }
    }

    [Fact]
    public void ValidateJson_ReturnsErrorsForLegacyArray()
    {
        var result = new JsonLedgerValidator().ValidateJson("[]");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("LedgerForge ledger v1 wrapper", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateJson_ReturnsErrorsForInvalidEvent()
    {
        const string json = """
        {
          "schemaVersion": "ledgerforge-ledger-v1",
          "metadata": {
            "createdAtUtc": "2026-07-02T12:00:00+00:00",
            "generator": "LedgerForge",
            "eventCount": 1
          },
          "events": [
            {
              "id": "00000000-0000-0000-0000-000000000000",
              "timestampUtc": "2026-07-02T12:00:00+02:00",
              "eventType": "Mystery",
              "description": "",
              "sourceReference": {
                "sourceSystem": "",
                "sourceFile": "",
                "sourceRowNumber": 0,
                "rawData": ""
              },
              "postings": [
                {
                  "assetSymbol": "",
                  "amount": 0,
                  "direction": "Sideways",
                  "account": ""
                }
              ]
            }
          ]
        }
        """;

        var result = new JsonLedgerValidator().ValidateJson(json);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("$.events[0].id", StringComparison.Ordinal));
        Assert.Contains(result.Errors, e => e.Contains("$.events[0].eventType", StringComparison.Ordinal));
        Assert.Contains(result.Errors, e => e.Contains("$.events[0].postings[0].amount", StringComparison.Ordinal));
    }

    [Fact]
    public async Task JsonLedgerStore_ReadAsync_SupportsLegacyEventArray()
    {
        var outputFolder = Directory.CreateTempSubdirectory("ledgerforge-legacy-ledger-");
        try
        {
            var ledgerPath = Path.Combine(outputFolder.FullName, "legacy-ledger.json");
            await File.WriteAllTextAsync(
                ledgerPath,
                """
                [
                  {
                    "id": "11111111-1111-1111-1111-111111111111",
                    "timestampUtc": "2025-01-01T00:00:00+00:00",
                    "eventType": "Deposit",
                    "description": "Legacy deposit",
                    "sourceReference": {
                      "sourceSystem": "Test",
                      "sourceFile": "legacy.csv",
                      "sourceRowNumber": 1,
                      "rawData": "raw"
                    },
                    "postings": []
                  }
                ]
                """);

            var events = await new JsonLedgerStore().ReadAsync(ledgerPath);

            var ledgerEvent = Assert.Single(events);
            Assert.Equal(LedgerEventType.Deposit, ledgerEvent.EventType);
        }
        finally
        {
            outputFolder.Delete(recursive: true);
        }
    }

    private static LedgerEvent CreateEvent()
    {
        return new LedgerEvent(
            Guid.NewGuid(),
            new DateTimeOffset(2026, 7, 2, 12, 0, 0, TimeSpan.Zero),
            LedgerEventType.Deposit,
            "Test deposit",
            new SourceReference("Test", "test.csv", 1, "raw"),
            new[]
            {
                new LedgerPosting("BTC", 1m, LedgerPostingDirection.In, "Test")
            });
    }
}
