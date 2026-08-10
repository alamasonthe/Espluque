using Espluque.Application.ModuleManager.Entities;
using System.Reflection;
using System.Runtime.Loader;
using Espluque.Contracts.ModuleInterfaces;

namespace Espluque.Application.ModuleManager.Services
{
    public class CatalogService
    {
        public static async Task<List<ICatalogEntry>> BuildAsync(string modulesRootPath)
        {
            ModuleService moduleService = new();

            try
            {
                List<ICatalogEntry> catalogEntries = [];

                var moduleInfoPaths = moduleService.GetModuleInfoPaths(modulesRootPath);

                foreach (string moduleInfoPath in moduleInfoPaths)
                {
                    IModuleInfo? moduleInfo = await moduleService.LoadModuleInfo(moduleInfoPath);

                    if (moduleInfo is null)
                    {
                        continue;
                    }

                    string moduleDirectoryPath = Path.GetDirectoryName(moduleInfoPath)!;
                    string assemblyPath = Path.GetFullPath( Path.Combine(moduleDirectoryPath, moduleInfo.Assembly));

                    List<ICatalogEntry> moduleCatalogEntries = await CreateCatalogEntriesAsync(moduleInfo, assemblyPath);

                    catalogEntries.AddRange(moduleCatalogEntries);
                }

                return catalogEntries;
            }
            catch
            {
                return [];
            }
        }

        private async static Task<List<ICatalogEntry>> CreateCatalogEntriesAsync(IModuleInfo moduleInfo, string assemblyPath)
        {
            try
            {
                if (moduleInfo is null
                    || string.IsNullOrWhiteSpace(moduleInfo.Name)
                    || string.IsNullOrWhiteSpace(moduleInfo.Assembly)
                    || moduleInfo.Contributions is null)
                {
                    return [];
                }

                if (string.IsNullOrWhiteSpace(assemblyPath) || !File.Exists(assemblyPath))
                {
                    return [];
                }

                // var context = new AssemblyContextLoader(assemblyPath);
                // var assembly = context.LoadFromAssemblyPath(assemblyPath);
                Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);

                List<ICatalogEntry> catalogEntries = [];

                foreach (IModuleContributionInfo contribution in moduleInfo.Contributions)
                {
                    if (string.IsNullOrWhiteSpace(contribution.InterfaceType)
                        || string.IsNullOrWhiteSpace(contribution.ClassName))
                    {
                        continue;
                    }

                    if (!contribution.ContributionSettings.Active) { continue; }

                    catalogEntries.Add(new CatalogEntry
                    {
                        InterfaceType = contribution.InterfaceType,
                        Label = contribution.Label,
                        ClassName = contribution.ClassName,
                        Tags = contribution.ContributionSettings.Tags ?? [],
                        AssemblyPath = assemblyPath,
                        Assembly = assembly,
                        ModuleName = moduleInfo.Name,
                        ModuleVersion = moduleInfo.Version
                    });
                }

                return catalogEntries;
            }
            catch
            {
                return [];
            }
        }

        public static List<ICatalogEntry> FilterCatalog(List<ICatalogEntry> catalog, string interfaceType, string tag)
        {
            List<ICatalogEntry> entries = catalog
                .Where(entry => string.Equals(entry.InterfaceType, interfaceType, StringComparison.OrdinalIgnoreCase)
                    && entry.Tags.Any(entryTag => string.Equals(entryTag, tag, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return entries;
        }

        public static Task UnloadContexts(List<CatalogEntry> catalog)
        {
            try
            {
                if (catalog == null || catalog.Count == 0) return Task.CompletedTask;

                List<Assembly> assemblies = catalog
                    .Select(entry => entry.Assembly)
                    .OfType<Assembly>()
                    .Distinct()
                    .ToList();

                foreach (Assembly assembly in assemblies)
                {
                    AssemblyLoadContext? context = AssemblyLoadContext.GetLoadContext(assembly);
                    context?.Unload();
                }
            }
            catch{ }
            return Task.CompletedTask;
        }
    }
}
