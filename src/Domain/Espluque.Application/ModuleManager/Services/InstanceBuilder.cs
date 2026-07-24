using Espluque.Application.ModuleManager.Entities;
using Espluque.Contracts.Interfaces;
using Espluque.Contracts.MessageInterfaces;
using Espluque.Contracts.Ports;
using Espluque.Contracts.ModuleInterfaces;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Espluque.Application.ModuleManager.Services
{
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

        /*
        public static object? CreateInstance(Assembly assembly, string className)
        {
            Type? classType = assembly.GetType(className);

            if (classType is null)
            {
                return null;
            }

            return Activator.CreateInstance(classType);
        }
        */

        public static (string label, object instance)? CreateInstance(
            ICatalogEntry catalogEntry,
            IMessageCenter messageCenter,
            Espluque.Contracts.Ports.ILogger logger,
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

                ConstructorInfo? constructor = classType.GetConstructor([typeof(IMessageCenter), typeof(Espluque.Contracts.Ports.ILogger), typeof(ISettingsService), typeof(IEntityFactory)]);
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
                Espluque.Contracts.Ports.ILogger logger,
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
