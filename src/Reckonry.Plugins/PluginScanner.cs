using System.Reflection;
using Reckonry.Importers.Abstractions;
using Reckonry.Pricing.Abstractions;
using Reckonry.Reconciliation.Abstractions;
using Reckonry.Reports;
using Reckonry.Tax.Abstractions;

namespace Reckonry.Plugins;

public static class PluginScanner
{
    public static PluginCatalog ScanPlugins(params Assembly[] assemblies)
    {
        var pluginAssemblies = assemblies.Length == 0
            ? LoadReckonryAssemblies()
            : assemblies.Distinct().ToArray();

        return new PluginCatalog(
            CreateAll<ISourceImporter>(pluginAssemblies),
            CreateAll<ITaxModule>(pluginAssemblies),
            CreateAll<IReportModule>(pluginAssemblies),
            CreateAll<IReconciliationModule>(pluginAssemblies),
            CreateAll<IPriceProvider>(pluginAssemblies));
    }

    private static IReadOnlyList<TPlugin> CreateAll<TPlugin>(IReadOnlyCollection<Assembly> assemblies)
    {
        return assemblies
            .SelectMany(GetLoadableTypes)
            .Where(type => typeof(TPlugin).IsAssignableFrom(type))
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .Select(type => (Type: type, Constructor: SelectConstructor(type)))
            .Where(candidate => candidate.Constructor is not null)
            .Select(candidate => (TPlugin)candidate.Constructor!.Invoke(BuildArguments(candidate.Constructor)))
            .OrderBy(plugin => plugin?.GetType().FullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static ConstructorInfo? SelectConstructor(Type type)
    {
        return type
            .GetConstructors()
            .OrderBy(constructor => constructor.GetParameters().Length)
            .FirstOrDefault(constructor => constructor.GetParameters().All(parameter => parameter.IsOptional));
    }

    private static object?[] BuildArguments(ConstructorInfo constructor)
    {
        return constructor
            .GetParameters()
            .Select(parameter => parameter.DefaultValue == DBNull.Value ? null : parameter.DefaultValue)
            .ToArray();
    }

    private static IReadOnlyList<Assembly> LoadReckonryAssemblies()
    {
        var loadedByName = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(assembly => assembly.GetName().Name?.StartsWith("Reckonry.", StringComparison.OrdinalIgnoreCase) == true)
            .ToDictionary(assembly => assembly.GetName().Name!, StringComparer.OrdinalIgnoreCase);

        var baseDirectory = AppContext.BaseDirectory;
        if (Directory.Exists(baseDirectory))
        {
            foreach (var path in Directory.EnumerateFiles(baseDirectory, "Reckonry.*.dll", SearchOption.TopDirectoryOnly))
            {
                var assemblyName = Path.GetFileNameWithoutExtension(path);
                if (loadedByName.ContainsKey(assemblyName))
                {
                    continue;
                }

                try
                {
                    loadedByName[assemblyName] = Assembly.LoadFrom(path);
                }
                catch (BadImageFormatException)
                {
                }
                catch (FileLoadException)
                {
                }
            }
        }

        return loadedByName.Values.ToArray();
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type is not null)!;
        }
    }
}
