namespace LedgerForge.Importers.Abstractions;

public sealed record ImporterDescriptor
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public required string Provider { get; init; }

    public string Exchange => Provider;

    public required string ImporterVersion { get; init; }

    public required decimal CoveragePercent { get; init; }

    public required IReadOnlySet<string> SupportedFileExtensions { get; init; }

    public required IReadOnlyList<string> SupportedFiles { get; init; }

    public required IReadOnlyList<string> SupportedSchemas { get; init; }

    public required IReadOnlyList<string> SupportedOperations { get; init; }
}
