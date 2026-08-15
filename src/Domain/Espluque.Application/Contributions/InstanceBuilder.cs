using System.Reflection;
using Microsoft.Extensions.Logging;
using Espluque.Application.Catalog;
using Espluque.Contracts.Catalog;
using Espluque.Contracts.CrossCutting;

namespace Espluque.Application.Contributions
{
    /// <summary>
    /// Builds executable contribution instances from catalog entries.
    /// </summary>
    /// <remarks>
    /// Contribution types are resolved from the assembly and class name stored in the catalog entry.
    /// Contributions must expose a constructor accepting IMessageCenter, ILogger, ISettingsService and IEntityFactory.
    ///
    /// Multiple instances can be created by filtering the catalog by contribution interface type and thesaurus tag.
    /// Type resolution for contribution interfaces targets the Espluque.Contracts.ModuleInterfaces namespace
    /// in the Espluque.Contracts assembly.
    ///
    /// Resolution or instantiation failures return no instance and are logged when a logger is available.
    /// </remarks>

    public class InstanceBuilder
    {
        public InstanceBuilder()
        {
        }


        public static Type? GetType(string typeName)
        {
            try
            {
                string interfaceNamespace = "Espluque.Contracts.ModuleInterfaces";
                string interfaceAssemblyName = "Espluque.Contracts";

                string fullTypeName = $"{interfaceNamespace}.{typeName.Trim()}";

                Type? interfaceType = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(assembly => assembly.GetName().Name == interfaceAssemblyName)
                    .Select(assembly => assembly.GetType(fullTypeName))
                    .FirstOrDefault(type => type is not null);

                return interfaceType;
            }
            catch
            {
                return null;
            }
        }

        public static (string label, object instance)? CreateInstance(
            ICatalogEntry catalogEntry,
            IMessageCenter messageCenter,
            Contracts.CrossCutting.ILogger logger,
            ISettingsService settingsService,
            IEntityFactory entityFactory)
        {
            try
            {
                Type? classType = catalogEntry.Assembly?.GetType(catalogEntry.ClassName);
                if (classType is null)
                {
                    logger.Log(
                        LogLevel.Error,
                        $"Module contribution type not found: Assembly={catalogEntry.AssemblyPath}, ClassName={catalogEntry.ClassName}");

                    return null;
                }

                ConstructorInfo? constructor = classType.GetConstructor([typeof(IMessageCenter), typeof(Contracts.CrossCutting.ILogger), typeof(ISettingsService), typeof(IEntityFactory)]);
                if (constructor is null)
                {
                    logger.Log(LogLevel.Debug, $"Module contribution constructor not found: ClassName={catalogEntry.ClassName}");
                    return null;
                }

                object? instance = constructor.Invoke([messageCenter, logger, settingsService, entityFactory]);
                if (instance is null) return null;

                return (catalogEntry.Label, instance);
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, $"Module contribution Create Instance: {ex.Message}");
                return null;
            }
        }

        public static async IAsyncEnumerable<(string label, object instance)> CreateInstancesAsync(
                List<ICatalogEntry> catalog,
                string interfaceType,
                string tag,
                IMessageCenter messageCenter,
                Contracts.CrossCutting.ILogger logger,
                ISettingsService settingsService,
                IEntityFactory entityFactory)
        {
            List<ICatalogEntry> entries = CatalogService.FilterCatalog(catalog, interfaceType, tag);

            foreach (CatalogEntry entry in entries)
            {
                (string label, object instance)? contribution = CreateInstance(entry, messageCenter, logger, settingsService, entityFactory);

                if (contribution is not null)
                {
                    yield return contribution.Value;
                }
            }
        }
    }
}
