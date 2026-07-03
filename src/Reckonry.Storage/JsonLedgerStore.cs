using System.Text.Json;
using System.Text.Json.Serialization;
using Reckonry.Core;

namespace Reckonry.Storage;

public sealed class JsonLedgerStore : ILedgerStore
{
    private readonly ILedgerValidator validator;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonLedgerStore()
        : this(new JsonLedgerValidator())
    {
    }

    public JsonLedgerStore(ILedgerValidator validator)
    {
        this.validator = validator;
    }

    public async Task<IReadOnlyList<LedgerEvent>> ReadAsync(
        string ledgerJsonPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ledgerJsonPath);

        await using var stream = File.OpenRead(ledgerJsonPath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            return JsonSerializer.Deserialize<IReadOnlyList<LedgerEvent>>(document.RootElement.GetRawText(), JsonOptions)
                ?? Array.Empty<LedgerEvent>();
        }

        if (document.RootElement.ValueKind == JsonValueKind.Object
            && document.RootElement.TryGetProperty("events", out var events))
        {
            return JsonSerializer.Deserialize<IReadOnlyList<LedgerEvent>>(events.GetRawText(), JsonOptions)
                ?? Array.Empty<LedgerEvent>();
        }

        throw new InvalidDataException("Ledger JSON must be a Reckonry v1 ledger object or a legacy event array.");
    }

    public async Task WriteAsync(
        string ledgerJsonPath,
        IReadOnlyCollection<LedgerEvent> events,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ledgerJsonPath);
        ArgumentNullException.ThrowIfNull(events);

        var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(ledgerJsonPath));
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var ledger = new Ledger(
            JsonLedgerValidator.CurrentSchemaVersion,
            new LedgerMetadata(DateTimeOffset.UtcNow, "Reckonry", events.Count),
            events.ToArray());

        await using (var stream = File.Create(ledgerJsonPath))
        {
            await JsonSerializer.SerializeAsync(stream, ledger, JsonOptions, cancellationToken);
        }

        var validation = await validator.ValidateFileAsync(ledgerJsonPath, cancellationToken);
        if (!validation.IsValid)
        {
            throw new InvalidDataException(
                "Generated ledger.json failed canonical validation: " + string.Join("; ", validation.Errors));
        }
    }
}
