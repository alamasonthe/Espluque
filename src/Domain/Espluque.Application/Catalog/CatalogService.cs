using Espluque.Application.Modules;
using Espluque.Contracts.Catalog;
using Espluque.Contracts.Contributions;
using Espluque.Contracts.CrossCutting;
using Espluque.Contracts.Modules;
using System.Reflection;
using System.Runtime.Loader;

namespace Espluque.Application.Catalog
{
    /// <summary>
    /// Builds and manages the runtime catalog of active module contributions.
    /// </summary>
    /// <remarks>
    /// BuildAsync loads module descriptors and creates one catalog entry per active contribution.
    /// FilterCatalog selects entries by contribution interface type and tag.
    /// UnloadContexts unloads the assembly contexts associated with catalog entries.
    /// </remarks>

    public class CatalogService
    {
        private readonly ISettingsService _settingsService;

        public CatalogService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public async Task<List<ICatalogEntry>> BuildAsync(string modulesRootPath)
        {
            ModuleService moduleService = new(_settingsService);

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

        private async Task<List<ICatalogEntry>> CreateCatalogEntriesAsync(IModuleInfo moduleInfo, string assemblyPath)
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

                Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);

                List<ICatalogEntry> catalogEntries = [];

                foreach (IModuleContributionInfo contribution in moduleInfo.Contributions)
                {
                    if (string.IsNullOrWhiteSpace(contribution.InterfaceType)
                        || string.IsNullOrWhiteSpace(contribution.ClassName))
                    {
                        continue;
                    }

                    if (!contribution.ContributionSettings.Active)
                    {
                        continue;
                    }

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
