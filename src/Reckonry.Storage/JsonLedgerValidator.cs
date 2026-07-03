using System.Text.Json;
using Reckonry.Core;

namespace Reckonry.Storage;

public sealed class JsonLedgerValidator : ILedgerValidator
{
    public const string CurrentSchemaVersion = "reckonry-ledger-v1";

    private static readonly HashSet<string> EventTypes = Enum.GetNames<LedgerEventType>().ToHashSet(StringComparer.Ordinal);
    private static readonly HashSet<string> PostingDirections = Enum.GetNames<LedgerPostingDirection>().ToHashSet(StringComparer.Ordinal);

    public async Task<LedgerValidationResult> ValidateFileAsync(
        string ledgerJsonPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ledgerJsonPath);

        if (!File.Exists(ledgerJsonPath))
        {
            return new LedgerValidationResult([$"File does not exist: {ledgerJsonPath}"]);
        }

        var json = await File.ReadAllTextAsync(ledgerJsonPath, cancellationToken);
        return ValidateJson(json);
    }

    public LedgerValidationResult ValidateJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var errors = new List<string>();
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            return new LedgerValidationResult([$"Invalid JSON: {ex.Message}"]);
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return new LedgerValidationResult(["$ must be an object using the Reckonry ledger v1 wrapper."]);
            }

            ValidateRequiredString(root, "schemaVersion", "$.schemaVersion", errors, CurrentSchemaVersion);
            ValidateMetadata(root, errors);
            ValidateEvents(root, errors);
        }

        return errors.Count == 0 ? LedgerValidationResult.Pass : new LedgerValidationResult(errors);
    }

    private static void ValidateMetadata(JsonElement root, ICollection<string> errors)
    {
        if (!TryGetRequiredObject(root, "metadata", "$.metadata", errors, out var metadata))
        {
            return;
        }

        ValidateRequiredDateTimeOffset(metadata, "createdAtUtc", "$.metadata.createdAtUtc", errors);
        ValidateRequiredString(metadata, "generator", "$.metadata.generator", errors);
        ValidateRequiredNonNegativeInt(metadata, "eventCount", "$.metadata.eventCount", errors);
    }

    private static void ValidateEvents(JsonElement root, ICollection<string> errors)
    {
        if (!root.TryGetProperty("events", out var events))
        {
            errors.Add("$.events is required.");
            return;
        }

        if (events.ValueKind != JsonValueKind.Array)
        {
            errors.Add("$.events must be an array.");
            return;
        }

        var actualCount = events.GetArrayLength();
        if (root.TryGetProperty("metadata", out var metadata)
            && metadata.ValueKind == JsonValueKind.Object
            && metadata.TryGetProperty("eventCount", out var eventCount)
            && eventCount.ValueKind == JsonValueKind.Number
            && eventCount.TryGetInt32(out var declaredCount)
            && declaredCount != actualCount)
        {
            errors.Add("$.metadata.eventCount must match $.events length.");
        }

        var index = 0;
        foreach (var ledgerEvent in events.EnumerateArray())
        {
            ValidateEvent(ledgerEvent, $"$.events[{index}]", errors);
            index++;
        }
    }

    private static void ValidateEvent(JsonElement ledgerEvent, string path, ICollection<string> errors)
    {
        if (ledgerEvent.ValueKind != JsonValueKind.Object)
        {
            errors.Add($"{path} must be an object.");
            return;
        }

        ValidateRequiredGuid(ledgerEvent, "id", $"{path}.id", errors);
        ValidateRequiredDateTimeOffset(ledgerEvent, "timestampUtc", $"{path}.timestampUtc", errors, requireUtc: true);
        ValidateRequiredString(ledgerEvent, "eventType", $"{path}.eventType", errors, allowedValues: EventTypes);
        ValidateRequiredString(ledgerEvent, "description", $"{path}.description", errors);
        ValidateSourceReference(ledgerEvent, $"{path}.sourceReference", errors);
        ValidatePostings(ledgerEvent, $"{path}.postings", errors);
    }

    private static void ValidateSourceReference(JsonElement ledgerEvent, string path, ICollection<string> errors)
    {
        if (!TryGetRequiredObject(ledgerEvent, "sourceReference", path, errors, out var sourceReference))
        {
            return;
        }

        ValidateRequiredString(sourceReference, "sourceSystem", $"{path}.sourceSystem", errors);
        ValidateRequiredString(sourceReference, "sourceFile", $"{path}.sourceFile", errors);
        ValidateRequiredPositiveInt(sourceReference, "sourceRowNumber", $"{path}.sourceRowNumber", errors);
        ValidateRequiredString(sourceReference, "rawData", $"{path}.rawData", errors);
    }

    private static void ValidatePostings(JsonElement ledgerEvent, string path, ICollection<string> errors)
    {
        if (!ledgerEvent.TryGetProperty("postings", out var postings))
        {
            errors.Add($"{path} is required.");
            return;
        }

        if (postings.ValueKind != JsonValueKind.Array)
        {
            errors.Add($"{path} must be an array.");
            return;
        }

        var index = 0;
        foreach (var posting in postings.EnumerateArray())
        {
            ValidatePosting(posting, $"{path}[{index}]", errors);
            index++;
        }
    }

    private static void ValidatePosting(JsonElement posting, string path, ICollection<string> errors)
    {
        if (posting.ValueKind != JsonValueKind.Object)
        {
            errors.Add($"{path} must be an object.");
            return;
        }

        ValidateRequiredString(posting, "assetSymbol", $"{path}.assetSymbol", errors);
        ValidateRequiredPositiveDecimal(posting, "amount", $"{path}.amount", errors);
        ValidateRequiredString(posting, "direction", $"{path}.direction", errors, allowedValues: PostingDirections);
        ValidateRequiredString(posting, "account", $"{path}.account", errors);

        if (posting.TryGetProperty("value", out var value) && value.ValueKind is not JsonValueKind.Null)
        {
            ValidateMoney(value, $"{path}.value", errors);
        }
    }

    private static void ValidateMoney(JsonElement value, string path, ICollection<string> errors)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            errors.Add($"{path} must be an object or null.");
            return;
        }

        ValidateRequiredString(value, "currencyCode", $"{path}.currencyCode", errors, length: 3);
        ValidateRequiredNonNegativeDecimal(value, "amount", $"{path}.amount", errors);
    }

    private static bool TryGetRequiredObject(JsonElement parent, string propertyName, string path, ICollection<string> errors, out JsonElement value)
    {
        if (!parent.TryGetProperty(propertyName, out value))
        {
            errors.Add($"{path} is required.");
            return false;
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            errors.Add($"{path} must be an object.");
            return false;
        }

        return true;
    }

    private static void ValidateRequiredString(
        JsonElement parent,
        string propertyName,
        string path,
        ICollection<string> errors,
        string? requiredValue = null,
        IReadOnlySet<string>? allowedValues = null,
        int? length = null)
    {
        if (!parent.TryGetProperty(propertyName, out var value))
        {
            errors.Add($"{path} is required.");
            return;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            errors.Add($"{path} must be a string.");
            return;
        }

        var text = value.GetString();
        if (string.IsNullOrWhiteSpace(text))
        {
            errors.Add($"{path} must not be blank.");
            return;
        }

        if (requiredValue is not null && !string.Equals(text, requiredValue, StringComparison.Ordinal))
        {
            errors.Add($"{path} must be '{requiredValue}'.");
        }

        if (allowedValues is not null && !allowedValues.Contains(text))
        {
            errors.Add($"{path} has an unsupported value.");
        }

        if (length is not null && text.Length != length.Value)
        {
            errors.Add($"{path} must be {length.Value} characters long.");
        }
    }

    private static void ValidateRequiredGuid(JsonElement parent, string propertyName, string path, ICollection<string> errors)
    {
        if (!parent.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String || !Guid.TryParse(value.GetString(), out var guid) || guid == Guid.Empty)
        {
            errors.Add($"{path} must be a non-empty GUID string.");
        }
    }

    private static void ValidateRequiredDateTimeOffset(
        JsonElement parent,
        string propertyName,
        string path,
        ICollection<string> errors,
        bool requireUtc = false)
    {
        if (!parent.TryGetProperty(propertyName, out var value)
            || value.ValueKind != JsonValueKind.String
            || !DateTimeOffset.TryParse(value.GetString(), out var timestamp))
        {
            errors.Add($"{path} must be an ISO-8601 timestamp string.");
            return;
        }

        if (requireUtc && timestamp.Offset != TimeSpan.Zero)
        {
            errors.Add($"{path} must be UTC with zero offset.");
        }
    }

    private static void ValidateRequiredPositiveInt(JsonElement parent, string propertyName, string path, ICollection<string> errors)
    {
        if (!parent.TryGetProperty(propertyName, out var value)
            || value.ValueKind != JsonValueKind.Number
            || !value.TryGetInt32(out var number)
            || number <= 0)
        {
            errors.Add($"{path} must be a positive integer.");
        }
    }

    private static void ValidateRequiredNonNegativeInt(JsonElement parent, string propertyName, string path, ICollection<string> errors)
    {
        if (!parent.TryGetProperty(propertyName, out var value)
            || value.ValueKind != JsonValueKind.Number
            || !value.TryGetInt32(out var number)
            || number < 0)
        {
            errors.Add($"{path} must be a non-negative integer.");
        }
    }

    private static void ValidateRequiredPositiveDecimal(JsonElement parent, string propertyName, string path, ICollection<string> errors)
    {
        if (!TryGetRequiredDecimal(parent, propertyName, path, errors, out var value))
        {
            return;
        }

        if (value <= 0)
        {
            errors.Add($"{path} must be greater than zero.");
        }
    }

    private static void ValidateRequiredNonNegativeDecimal(JsonElement parent, string propertyName, string path, ICollection<string> errors)
    {
        if (!TryGetRequiredDecimal(parent, propertyName, path, errors, out var value))
        {
            return;
        }

        if (value < 0)
        {
            errors.Add($"{path} must be greater than or equal to zero.");
        }
    }

    private static bool TryGetRequiredDecimal(JsonElement parent, string propertyName, string path, ICollection<string> errors, out decimal value)
    {
        value = default;
        if (!parent.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.Number || !element.TryGetDecimal(out value))
        {
            errors.Add($"{path} must be a JSON number parseable as decimal.");
            return false;
        }

        return true;
    }
}
