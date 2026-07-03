using System.Text.Json;
using System.Text.Json.Serialization;
using Reckonry.Core;

namespace Reckonry.Tax.Italy.Rw;

public sealed class ItalyRwConfigWorkflow : IItalyRwConfigWorkflow
{
    private const string DefaultReconciliationSummaryPath = "output/reconciliation/reconciliation-summary.json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<ItalyRwConfigWorkflowResult> WriteTemplateAsync(
        int year,
        IReadOnlyCollection<LedgerEvent> ledgerEvents,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ledgerEvents);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var assets = DetectCandidateCryptoAssetSymbols(ledgerEvents)
            .Select(asset => new ItalyRwPrivateAssetConfig
            {
                AssetSymbol = asset,
                ValuationCriterion = null,
                InitialValue = NewEmptyEvidence(),
                FinalValue = NewEmptyEvidence()
            })
            .ToArray();

        var config = new ItalyRwPrivateConfig
        {
            Year = year,
            TaxpayerConfiguration = new ItalyRwTaxpayerConfiguration
            {
                OwnershipTitle = null,
                PossessionType = null,
                OwnershipPercentage = null,
                MonitoringOnly = null,
                ForeignStateCode = null,
                ForeignStateTreatmentNotes = null
            },
            Rw8Inputs = new ItalyRw8InputConfiguration
            {
                PriorCryptoTaxCredit = null,
                CryptoTaxF24Compensations = null,
                CryptoTaxAdvancesPaid = null
            },
            ReconciliationSummaryPath = DefaultReconciliationSummaryPath,
            Assets = assets,
            Warnings = new[]
            {
                "Template contains placeholders only. Do not file until taxpayer configuration and valuation evidence are reviewed."
            }
        };

        await WriteConfigAsync(outputPath, config, cancellationToken);
        return BuildResult(outputPath, config);
    }

    public async Task<ItalyRwConfigWorkflowResult> FillFromBinanceAsync(
        string configPath,
        string reconciliationSummaryPath,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(reconciliationSummaryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var config = await ReadConfigAsync(configPath, cancellationToken);
        var warnings = config.Warnings.ToList();
        warnings.AddRange(await BuildBinanceFillWarningsAsync(reconciliationSummaryPath, cancellationToken));

        var filledConfig = config with
        {
            ReconciliationSummaryPath = reconciliationSummaryPath,
            Warnings = warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
        };

        await WriteConfigAsync(outputPath, filledConfig, cancellationToken);
        return BuildResult(outputPath, filledConfig);
    }

    private static async Task<IReadOnlyList<string>> BuildBinanceFillWarningsAsync(
        string reconciliationSummaryPath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(reconciliationSummaryPath))
        {
            return new[]
            {
                "Binance reconciliation summary was not found. No valuation evidence was filled."
            };
        }

        await using var stream = File.OpenRead(reconciliationSummaryPath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("documents", out var documents)
            || documents.ValueKind != JsonValueKind.Array)
        {
            return new[]
            {
                "Binance reconciliation summary could not be read as a document summary. No valuation evidence was filled."
            };
        }

        var hasExtractedOfficialReport = documents
            .EnumerateArray()
            .Any(item =>
                item.TryGetProperty("extractionSucceeded", out var extraction)
                && extraction.ValueKind == JsonValueKind.True);

        if (!hasExtractedOfficialReport)
        {
            return new[]
            {
                "No extracted Binance official report was available. No valuation evidence was filled."
            };
        }

        return new[]
        {
            "Binance reconciliation summary contains report-level metadata but no unambiguous per-asset RW valuation fields. No valuation evidence was filled."
        };
    }

    private static async Task<ItalyRwPrivateConfig> ReadConfigAsync(
        string configPath,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(configPath);
        return await JsonSerializer.DeserializeAsync<ItalyRwPrivateConfig>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidDataException("Italy RW config could not be read.");
    }

    private static async Task WriteConfigAsync(
        string outputPath,
        ItalyRwPrivateConfig config,
        CancellationToken cancellationToken)
    {
        var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        await using var stream = File.Create(outputPath);
        await JsonSerializer.SerializeAsync(stream, config, JsonOptions, cancellationToken);
    }

    private static ItalyRwConfigWorkflowResult BuildResult(
        string outputPath,
        ItalyRwPrivateConfig config)
    {
        var filledValuations = config.Assets.Sum(asset =>
            CountFilledEvidence(asset.InitialValue) + CountFilledEvidence(asset.FinalValue));
        var totalValuationSlots = config.Assets.Count * 2;

        return new ItalyRwConfigWorkflowResult(
            Path.GetFileName(outputPath),
            config.Assets.Count,
            filledValuations,
            totalValuationSlots - filledValuations,
            config.Warnings.Count);
    }

    private static int CountFilledEvidence(ItalyRwPrivateValuationEvidence? evidence)
    {
        return IsEvidenceComplete(evidence) ? 1 : 0;
    }

    private static bool IsEvidenceComplete(ItalyRwPrivateValuationEvidence? evidence)
    {
        return evidence is not null
            && !string.IsNullOrWhiteSpace(evidence.Type)
            && evidence.ValueEur is not null
            && !string.IsNullOrWhiteSpace(evidence.SourceName)
            && evidence.SourceTimestamp is not null
            && evidence.Confidence is not null;
    }

    private static ItalyRwPrivateValuationEvidence NewEmptyEvidence()
    {
        return new ItalyRwPrivateValuationEvidence
        {
            Type = null,
            ValueEur = null,
            SourceName = null,
            SourceTimestamp = null,
            Confidence = null,
            Notes = null
        };
    }

    private static IReadOnlyCollection<string> DetectCandidateCryptoAssetSymbols(
        IReadOnlyCollection<LedgerEvent> ledgerEvents)
    {
        return ledgerEvents
            .SelectMany(ledgerEvent => ledgerEvent.Postings)
            .Select(posting => posting.AssetSymbol)
            .Where(asset => !string.IsNullOrWhiteSpace(asset))
            .Select(asset => asset.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(asset => asset, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
