namespace Reckonry.Tax.Abstractions;

public sealed record TaxRequiredInput(
    string Id,
    string DisplayName,
    string Description,
    bool Required);
