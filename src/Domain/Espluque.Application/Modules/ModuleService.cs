using Espluque.Application.Contributions;
using Espluque.Contracts.Catalog;
using Espluque.Contracts.Contributions;
using Espluque.Contracts.Contributions.Types;
using Espluque.Contracts.Modules;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Espluque.Contracts.CrossCutting;
using Microsoft.Extensions.Logging;

namespace Espluque.Application.Modules
{
    public class ModuleService : IModuleService
    {
        private readonly IContributionSettingsService _contributionSettingsService;
        private readonly Espluque.Contracts.CrossCutting.ILogger _logger;

        public ModuleService(
    IContributionSettingsService contributionSettingsService, Espluque.Contracts.CrossCutting.ILogger logger)
        {
            _contributionSettingsService = contributionSettingsService;
            _logger = logger;
        }


        public List<string> GetModuleInfoPaths(string modulesRootPath)
        {
            string[] moduleInfoPaths = Directory.GetFiles(modulesRootPath, "module.json", SearchOption.AllDirectories);
            return moduleInfoPaths.ToList();
        }

        public async Task<IModuleInfo?> LoadModuleInfo(string moduleInfoPath)
        {
            try
            {
                string json = await File.ReadAllTextAsync(moduleInfoPath);
                JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true };

                jsonOptions.Converters.Add(new ModuleContributionJsonConverter());
                jsonOptions.Converters.Add(new ContributionSettingsJsonConverter());

                ModuleInfo? moduleInfo = JsonSerializer.Deserialize<ModuleInfo>(json, jsonOptions);
                if (moduleInfo is null)
                {
                    return null;
                }
                moduleInfo.FilePath = moduleInfoPath;


                foreach (IModuleContributionInfo contribution in moduleInfo.Contributions)
                {
                    IContributionSettings? userSettings =
                        await _contributionSettingsService.GetUserSettings(
                            moduleInfo.Assembly,
                            contribution.InterfaceType,
                            contribution.ClassName);

                    if (userSettings is not null)
                    {
                        contribution.ContributionSettings = userSettings;
                    }
                }

                return moduleInfo;
            }
            catch (Exception ex)
            {
                _logger.Log( LogLevel.Error, $"ModuleService: Cannot load module '{moduleInfoPath}': {ex.Message}");
                return null;
            }
        }

        public async Task<bool> SaveModuleInfo(IModuleInfo moduleInfo, string? filePath = null)
        {
            try
            {
                JsonSerializerOptions jsonOptions = new()
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };

                jsonOptions.Converters.Add(new ModuleContributionJsonConverter());
                jsonOptions.Converters.Add(new ContributionSettingsJsonConverter());

                string json = JsonSerializer.Serialize(
                    moduleInfo,
                    moduleInfo.GetType(),
                    jsonOptions);

                string targetPath = string.IsNullOrWhiteSpace(filePath)
                    ? moduleInfo.FilePath
                    : filePath;

                await File.WriteAllTextAsync(targetPath, json);

                return true;
            }
            catch (Exception ex)
            {
                // log!
                return false;
            }
        }

        public async Task<List<IModuleInfo>> GetModuleInfoList(List<string> moduleInfoPaths)
        {
            List<IModuleInfo> moduleInfos = [];

            foreach (string moduleInfoPath in moduleInfoPaths)
            {
                var moduleInfo = await LoadModuleInfo(moduleInfoPath);
                if (moduleInfo is not null)
                {
                    moduleInfos.Add(moduleInfo);
                }
            }

            return moduleInfos;
        }


        public static Task LoadModuleDependenciesAsync(List<ICatalogEntry> catalog)
        {
            return Task.Run(() => LoadDependenciesAsync(catalog));
        }

        private static async Task LoadDependenciesAsync(List<ICatalogEntry> catalog)
        {
            List<ICatalogEntry> dependencyEntries = catalog.Where(entry => entry.InterfaceType == nameof(IManagedDependencies)).ToList();

            foreach (ICatalogEntry entry in dependencyEntries)
            {
                if (!catalog.Contains(entry) || (entry.Assembly is null))
                {
                    continue;
                }

                Type? contributionType = entry.Assembly.GetType( entry.ClassName, throwOnError: false, ignoreCase: false);

                if (contributionType is null)
                {
                    catalog.RemoveAll(catalogEntry => string.Equals( catalogEntry.AssemblyPath, entry.AssemblyPath, StringComparison.OrdinalIgnoreCase));
                    continue;
                }

                IManagedDependencies managedDependencies;

                try
                {
                    if (Activator.CreateInstance(contributionType) is not IManagedDependencies instance)
                    {
                        catalog.RemoveAll(catalogEntry => string.Equals( catalogEntry.AssemblyPath, entry.AssemblyPath, StringComparison.OrdinalIgnoreCase));
                        continue;
                    }

                    managedDependencies = instance;
                }
                catch (Exception ex)
                {
                    // log ex.Message
                    catalog.RemoveAll(catalogEntry => string.Equals( catalogEntry.AssemblyPath, entry.AssemblyPath, StringComparison.OrdinalIgnoreCase));
                    continue;
                }

                foreach (string dependencyPath in managedDependencies.GetDependencyPaths())
                {
                    if (!File.Exists(dependencyPath))
                    {
                        continue;
                    }

                    try
                    {
                        AssemblyName dependencyName = AssemblyName.GetAssemblyName(dependencyPath);

                        bool alreadyLoaded = AssemblyLoadContext.Default.Assemblies.Any(
                            assembly => string.Equals( assembly.GetName().FullName, dependencyName.FullName, StringComparison.Ordinal));

                        if (alreadyLoaded)
                        {
                            continue;
                        }

                        AssemblyLoadContext.Default.LoadFromAssemblyPath(dependencyPath);
                    }
                    catch (Exception ex)
                    {
                        // log ex.Message
                    }
                }
            }
        }
    }
}
