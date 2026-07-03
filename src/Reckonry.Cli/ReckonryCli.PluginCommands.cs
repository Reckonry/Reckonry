using Reckonry.Core;
using Reckonry.Importers.Abstractions;
using Reckonry.Tax.Italy.Rw;
using System.Reflection;

internal static partial class ReckonryCli
{
    private static int ListImporters(AppServices services)
    {
        Console.WriteLine("Installed source importers:");
        foreach (var descriptor in services.ImporterFactory.ListImporters())
        {
            Console.WriteLine(
                $"{descriptor.Id} | {descriptor.SourceKind} | {descriptor.DisplayName} | Version {descriptor.ImporterVersion} | Coverage {descriptor.CoveragePercent:0.##}%");
            Console.WriteLine($"  Files: {FormatList(descriptor.SupportedFiles)}");
            Console.WriteLine($"  Schemas: {FormatList(descriptor.SupportedSchemas)}");
            Console.WriteLine($"  Operations: {FormatList(descriptor.SupportedOperations)}");
        }

        return ExitSuccess;
    }

    private static int ListPlugins(AppServices services)
    {
        var importers = services.ImporterFactory.ListImporters()
            .OrderBy(descriptor => descriptor.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var implementedImporters = importers
            .Where(descriptor => !IsPlaceholderImporter(descriptor))
            .ToArray();
        var plannedImporters = importers
            .Where(IsPlaceholderImporter)
            .ToArray();

        Console.WriteLine("Installed source importers:");
        if (implementedImporters.Length == 0)
        {
            Console.WriteLine("- None installed");
        }
        else
        {
            foreach (var descriptor in implementedImporters)
            {
                Console.WriteLine($"- {descriptor.Id} ({descriptor.SourceKind}) - {descriptor.DisplayName} [implemented; coverage {descriptor.CoveragePercent:0.##}%]");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Planned source importers:");
        if (plannedImporters.Length == 0)
        {
            Console.WriteLine("- None");
        }
        else
        {
            foreach (var descriptor in plannedImporters)
            {
                Console.WriteLine($"- {descriptor.Id} ({descriptor.SourceKind}) - {descriptor.DisplayName} [placeholder; not supported yet]");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Installed country modules:");
        var taxModules = services.TaxModules.Select(module => module.Descriptor).OrderBy(d => d.CountryCode).ToArray();
        if (taxModules.Length == 0)
        {
            Console.WriteLine("- None installed");
        }
        else
        {
            foreach (var descriptor in taxModules)
            {
                Console.WriteLine($"- {descriptor.CountryCode} - {descriptor.CountryName} ({descriptor.Version})");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Installed report modules:");
        var reportModules = services.Reports.Select(report => report.Descriptor).OrderBy(d => d.Id).ToArray();
        if (reportModules.Length == 0)
        {
            Console.WriteLine("- None installed");
        }
        else
        {
            foreach (var descriptor in reportModules)
            {
                var scope = descriptor.CountryCode is null
                    ? descriptor.Scope.ToString()
                    : $"{descriptor.Scope}; country={descriptor.CountryCode}";
                var provider = descriptor.ProviderId is null ? string.Empty : $"; provider={descriptor.ProviderId}";
                var review = descriptor.ProfessionalReviewRequired ? "; professional review required" : string.Empty;
                Console.WriteLine($"- {descriptor.Id} - {descriptor.DisplayName} ({scope}{provider}{review})");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Installed reconciliation modules:");
        var reconciliationModules = services.ReconciliationModules.Select(module => module.Descriptor).OrderBy(d => d.Id).ToArray();
        if (reconciliationModules.Length == 0)
        {
            Console.WriteLine("- None installed");
        }
        else
        {
            foreach (var descriptor in reconciliationModules)
            {
                Console.WriteLine($"- {descriptor.Id} - {descriptor.DisplayName} (provider={descriptor.ProviderId ?? "any"}; country={descriptor.CountryCode ?? "any"})");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Installed pricing providers:");
        var pricingProviders = services.Plugins.PricingProviders.OrderBy(provider => provider.ProviderId).ToArray();
        if (pricingProviders.Length == 0)
        {
            Console.WriteLine("- None installed");
        }
        else
        {
            foreach (var provider in pricingProviders)
            {
                Console.WriteLine($"- {provider.ProviderId}");
            }
        }

        return ExitSuccess;
    }

    private static bool IsPlaceholderImporter(ImporterDescriptor descriptor)
    {
        return descriptor.ImporterVersion.Contains("placeholder", StringComparison.OrdinalIgnoreCase)
            || descriptor.SupportedOperations.Contains("Planned", StringComparer.OrdinalIgnoreCase)
            || descriptor.SupportedSchemas.Contains("Planned", StringComparer.OrdinalIgnoreCase);
    }

    private static string FormatList(IEnumerable<string> values)
    {
        return string.Join(", ", values.Where(v => !string.IsNullOrWhiteSpace(v)));
    }
}
