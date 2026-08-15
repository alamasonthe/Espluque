using System.Reflection;
using System.Runtime.Loader;
using Espluque.Contracts.Modules;
using Espluque.Contracts.Thesaurus;
using Espluque.Contracts.CrossCutting;
using Espluque.Contracts.Contributions;

namespace Espluque.Application.Modules
{
    public class ModuleDiagService : IModuleDiagService
    {
        private static List<string> ContributionTypesRequiringTags { get; } =
        [
            "IDetector",
            "IExploiter",
            "IFusioner",
            "IGrabber",
            "IWpfViewer"
        ];

        private readonly ModuleService _moduleService;
        private readonly IThesaurusService _thesaurusService;
        private readonly IEntityFactory _entityFactory;

        public ModuleDiagService(IThesaurusService thesaurusService, IEntityFactory entityFactory, ISettingsService settingsService)
        {
            _moduleService = new ModuleService(settingsService);
            _thesaurusService = thesaurusService;
            _entityFactory = entityFactory;
        }

        public async Task<(IModuleHealth ModuleHealth, List<IContributionHealth> ContributionHealths)> DiagAsync(string filePath)
        {
            List<IContributionHealth> contributionHealths = [];

            (IModuleInfo? moduleInfo, IModuleHealth moduleHealth) = await DiagnoseModuleAsync(filePath);

            if (moduleHealth.HealthCheck != ModuleHealthCheckEnum.Success)
            {
                return (moduleHealth, []);
            }

            foreach (var contribution in moduleInfo!.Contributions)
            {
                IContributionHealth contributionHealth = await DiagnoseContribAsync(moduleInfo, contribution);
                contributionHealths.Add(contributionHealth);
            }

            return (moduleHealth, contributionHealths);
        }

        private async Task<(IModuleInfo? ModuleInfo, IModuleHealth ModuleHealth)> DiagnoseModuleAsync(string filePath)
        {
            string moduleName = Path.GetFileName(Path.GetDirectoryName(filePath)) ?? string.Empty;

            IModuleHealth moduleHealth = _entityFactory.CreateModuleHealth(
                moduleName,
                ModuleHealthCheckEnum.Success,
                null);

            List<string> diag = [];
            IModuleInfo? moduleInfo = null;

            try
            {
                // 1 - Read definition file
                try
                {
                    moduleInfo = await _moduleService.LoadModuleInfo(filePath);

                    if (moduleInfo is null)
                    {
                        diag.Add($"Read definition file: ERROR - Unable to read {filePath}");
                        moduleHealth.HealthCheck = ModuleHealthCheckEnum.Error;
                        moduleHealth.Diag = string.Join(Environment.NewLine, diag);
                        return (null, moduleHealth);
                    }

                    moduleHealth.ModuleName = moduleInfo.Name;
                    diag.Add("Read definition file: OK");
                }
                catch (Exception ex)
                {
                    diag.Add($"Read definition file: ERROR - {ex.GetBaseException().Message}");
                    moduleHealth.HealthCheck = ModuleHealthCheckEnum.Error;
                    moduleHealth.Diag = string.Join(Environment.NewLine, diag);
                    return (null, moduleHealth);
                }

                // 2 - Check assembly file
                string assemblyPath;

                try
                {
                    string moduleFolderPath = Path.GetDirectoryName(filePath)
                        ?? throw new DirectoryNotFoundException($"Module folder not found: {filePath}");

                    assemblyPath = Path.Combine(moduleFolderPath, moduleInfo.Assembly);

                    if (!File.Exists(assemblyPath))
                    {
                        diag.Add($"Check assembly file: ERROR - File not found: {assemblyPath}");
                        moduleHealth.HealthCheck = ModuleHealthCheckEnum.Error;
                        moduleHealth.Diag = string.Join(Environment.NewLine, diag);
                        return (moduleInfo, moduleHealth);
                    }

                    diag.Add("Check assembly file: OK");
                }
                catch (Exception ex)
                {
                    diag.Add($"Check assembly file: ERROR - {ex.GetBaseException().Message}");
                    moduleHealth.HealthCheck = ModuleHealthCheckEnum.Error;
                    moduleHealth.Diag = string.Join(Environment.NewLine, diag);
                    return (moduleInfo, moduleHealth);
                }

                // 3 - Load assembly
                try
                {
                    string fullAssemblyPath = Path.GetFullPath(assemblyPath);

                    Assembly? moduleAssembly = AssemblyLoadContext.Default.Assemblies
                        .FirstOrDefault(assembly =>
                            !assembly.IsDynamic
                            && !string.IsNullOrWhiteSpace(assembly.Location)
                            && string.Equals(
                                Path.GetFullPath(assembly.Location),
                                fullAssemblyPath,
                                StringComparison.OrdinalIgnoreCase));

                    if (moduleAssembly is null)
                    {
                        AssemblyLoadContext.Default.LoadFromAssemblyPath(fullAssemblyPath);
                    }

                    diag.Add("Load assembly: OK");
                }
                catch (Exception ex)
                {
                    diag.Add($"Load assembly: ERROR - {ex.GetBaseException().Message}");
                    moduleHealth.HealthCheck = ModuleHealthCheckEnum.Error;
                    moduleHealth.Diag = string.Join(Environment.NewLine, diag);
                    return (moduleInfo, moduleHealth);
                }
            }
            catch (Exception ex)
            {
                diag.Add($"Unexpected diagnostic error: {ex.GetBaseException().Message}");
                moduleHealth.HealthCheck = ModuleHealthCheckEnum.Error;
            }

            moduleHealth.Diag = string.Join(Environment.NewLine, diag);

            return (moduleInfo, moduleHealth);
        }

        private async Task<IContributionHealth> DiagnoseContribAsync(IModuleInfo moduleInfo, IModuleContributionInfo contribution)
        {
            IContributionHealth contributionHealth = _entityFactory.CreateContributionHealth(
                moduleInfo.Name,
                contribution.InterfaceType,
                contribution.ClassName,
                ModuleHealthCheckEnum.Success,
                null);

            List<string> diag = [];

            try
            {
                // 1 - Check contribution class
                try
                {
                    string assemblyPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(moduleInfo.FilePath)!, moduleInfo.Assembly));

                    Assembly moduleAssembly = AssemblyLoadContext.Default.Assemblies
                        .First(assembly =>
                            !assembly.IsDynamic
                            && !string.IsNullOrWhiteSpace(assembly.Location)
                            && string.Equals(
                                Path.GetFullPath(assembly.Location),
                                assemblyPath,
                                StringComparison.OrdinalIgnoreCase));

                    Type? contributionType = moduleAssembly.GetType(
                        contribution.ClassName,
                        throwOnError: false,
                        ignoreCase: false);

                    if (contributionType is null)
                    {
                        diag.Add($"Check contribution class: ERROR - Class not found: {contribution.ClassName}");
                        contributionHealth.HealthCheck = ModuleHealthCheckEnum.Error;
                    }
                    else
                    {
                        diag.Add("Check contribution class: OK");
                    }
                }
                catch (Exception ex)
                {
                    diag.Add($"Check contribution class: ERROR - {ex.GetBaseException().Message}");
                    contributionHealth.HealthCheck = ModuleHealthCheckEnum.Error;
                }

                // 2 - Check contribution tags
                if (ContributionTypesRequiringTags.Contains(contribution.InterfaceType))
                {
                    try
                    {
                        bool preferredTagFound = false;

                        foreach (string tag in contribution.ContributionSettings.Tags)
                        {
                            (int ConceptId, string MainTerm)? concept = await _thesaurusService.GetConceptMainTermByTerm("Espluque", tag);

                            if (concept is not null
                                && string.Equals(concept.Value.MainTerm, tag, StringComparison.OrdinalIgnoreCase))
                            {
                                preferredTagFound = true;
                                diag.Add($"Check contribution tags: OK");
                                break;
                            }
                        }

                        if (!preferredTagFound)
                        {
                            string message = contribution.ContributionSettings.Tags.Count == 0
                                ? "No tag is defined for this contribution."
                                : $"None of the contribution tags is a preferred thesaurus term: {string.Join(", ", contribution.ContributionSettings.Tags)}";

                            diag.Add($"Check contribution tags: ERROR - {message}");
                            contributionHealth.HealthCheck = ModuleHealthCheckEnum.Error;
                        }
                    }
                    catch (Exception ex)
                    {
                        diag.Add($"Check contribution tags: ERROR - {ex.GetBaseException().Message}");
                        contributionHealth.HealthCheck = ModuleHealthCheckEnum.Error;
                    }
                }

            }
            catch (Exception ex)
            {
                diag.Add($"Unexpected diagnostic error: {ex.GetBaseException().Message}");
                contributionHealth.HealthCheck = ModuleHealthCheckEnum.Error;
            }

            contributionHealth.Diag =
            diag.Count == 0
                ? null
                : string.Join(Environment.NewLine, diag);

            return contributionHealth;
        }
    }
}
