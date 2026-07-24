using Espluque.Application.ModuleManager.Entities;
using Espluque.Contracts.ModuleInterfaces;
using Espluque.Contracts.ModuleInterfaces.Contributions;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

namespace Espluque.Application.ModuleManager.Services
{
    public class ModuleService : IModuleService
    {
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

                ModuleInfo? moduleInfo = JsonSerializer.Deserialize<Application.ModuleManager.Entities.ModuleInfo>(json, jsonOptions);

                return moduleInfo;
            }
            catch (Exception ex)
            {
                // log!
                return null;
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
