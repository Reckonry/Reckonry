namespace Reckonry.Tax.Abstractions;

public sealed record TaxCompatibility(
    string LedgerSchema,
    string MinimumReckonryVersion,
    IReadOnlyList<string> Notes);
