namespace Reckonry.Tax.Abstractions;

public sealed record TaxOfficialSource(
    string Title,
    string Publisher,
    string? Url,
    string? Version,
    DateOnly? PublishedDate);
