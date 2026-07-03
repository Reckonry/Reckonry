namespace Reckonry.Importers.Abstractions;

public sealed class ImporterRegistry
{
    private readonly IReadOnlyList<IExchangeImporter> importers;
    private readonly Dictionary<string, IExchangeImporter> lookup;

    public ImporterRegistry(IEnumerable<IExchangeImporter> importers)
    {
        ArgumentNullException.ThrowIfNull(importers);

        this.importers = importers.OrderBy(i => i.Descriptor.Id, StringComparer.OrdinalIgnoreCase).ToArray();
        lookup = new Dictionary<string, IExchangeImporter>(StringComparer.OrdinalIgnoreCase);

        foreach (var importer in this.importers)
        {
            RegisterKey(importer.Descriptor.Id, importer);
            RegisterKey(importer.Descriptor.Provider, importer);
            RegisterKey(importer.Descriptor.DisplayName, importer);
            RegisterKey(NormalizeKey(importer.Descriptor.Provider), importer);
        }
    }

    public IReadOnlyList<ImporterDescriptor> ListDescriptors()
    {
        return importers.Select(i => i.Descriptor).ToArray();
    }

    public bool TryGet(string importerIdOrExchange, out IExchangeImporter importer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(importerIdOrExchange);

        return lookup.TryGetValue(importerIdOrExchange, out importer!)
            || lookup.TryGetValue(NormalizeKey(importerIdOrExchange), out importer!);
    }

    private void RegisterKey(string key, IExchangeImporter importer)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        var normalizedKey = key.Trim();
        if (lookup.TryGetValue(normalizedKey, out var existing) && !ReferenceEquals(existing, importer))
        {
            throw new InvalidOperationException($"Duplicate importer registration key: {normalizedKey}");
        }

        lookup[normalizedKey] = importer;
    }

    private static string NormalizeKey(string value)
    {
        return new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    }
}
