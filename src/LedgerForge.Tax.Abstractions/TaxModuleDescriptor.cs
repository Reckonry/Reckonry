namespace LedgerForge.Tax.Abstractions;

public sealed record TaxModuleDescriptor(
    string CountryCode,
    string DisplayName,
    string Version);
