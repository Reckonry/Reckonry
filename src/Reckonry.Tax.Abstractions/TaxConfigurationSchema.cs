namespace Reckonry.Tax.Abstractions;

public sealed record TaxConfigurationSchema(
    string Id,
    string DisplayName,
    string Format,
    string? SchemaPath);
