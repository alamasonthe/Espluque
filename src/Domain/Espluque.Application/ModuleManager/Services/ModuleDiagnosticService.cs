using Espluque.Application.ModuleManager.Adapters;
using Espluque.Application.ModuleManager.Entities;
using Espluque.Contracts.Enums;
using Espluque.Contracts.Interfaces;
using Espluque.Contracts.ModuleInterfaces;
using Espluque.Contracts.ModuleInterfaces.Contributions;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

namespace Espluque.Application.ModuleManager.Services
{
    public class ModuleDiagnosticService: IModuleDiagnosticService
    {
        private readonly IThesaurusService _thesaurusService;

        public ModuleDiagnosticService(IThesaurusService thesaurusService)
        {
            _thesaurusService = thesaurusService;
        }

        public async Task<IModuleDiagnostic> DiagnoseAsync(string filePath)
        {
            // try load module.json
            ModuleService moduleService = new();

            IModuleInfo? moduleInfo = await moduleService.LoadModuleInfo(filePath);

            if (moduleInfo is null)
            {
                string moduleName = Path.GetFileName(Path.GetDirectoryName(filePath));
                IModuleDiagnostic failedDiagnostic = new Application.Entities.Factory().CreateModuleDiagnostic(filePath, moduleName);
                failedDiagnostic.ModuleHealthCheck = ModuleHealthCheckEnum.Error;
                failedDiagnostic.ErrorDescription = $"Unable to load module definition from JSON file: {filePath}";
                return failedDiagnostic;
            }

            ModuleDiagnostic moduleDiagnostic = new()
            {
                FilePath = filePath,
                Name = moduleInfo.Name,
                Version = moduleInfo.Version,
                Assembly = moduleInfo.Assembly,
                Contributions = moduleInfo.Contributions.Select(ModuleContributionDiagnosticAdapter.ToDiagnostic).ToList()
            };

            string json = await File.ReadAllTextAsync(filePath);

            moduleDiagnostic.Json = json;

            JsonSerializerOptions jsonOptions = new()
            {
                PropertyNameCaseInsensitive = true
            };

            // file exists?

            string moduleFolderPath = Path.GetDirectoryName(filePath)!;
            string assemblyPath = Path.Combine(moduleFolderPath, moduleDiagnostic.Assembly);

            if (!File.Exists(assemblyPath))
            {
                moduleDiagnostic.ModuleHealthCheck = ModuleHealthCheckEnum.Error;
                moduleDiagnostic.ErrorDescription = $"Assembly file not found: {assemblyPath}";

                return moduleDiagnostic;
            }

            // Assembly load

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
                try
                {
                    moduleAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(fullAssemblyPath);
                }
                catch (Exception ex) when (ex is BadImageFormatException or FileLoadException or FileNotFoundException)
                {
                    moduleDiagnostic.ModuleHealthCheck = ModuleHealthCheckEnum.Error;
                    moduleDiagnostic.ErrorDescription = $"Unable to load assembly: {ex.GetBaseException().Message}";

                    return moduleDiagnostic;
                }
            }


            // foreach contribution vérif classe

            bool contributionHasError = false;

            foreach (IModuleContributionDiagnostic contribution in moduleDiagnostic.Contributions)
            {
                Type? contributionType = moduleAssembly.GetType(contribution.ClassName, throwOnError: false, ignoreCase: false);

                if (contributionType is null)
                {
                    contribution.ContributionHealthCheck = ModuleHealthCheckEnum.Error;
                    contribution.ErrorDescription = $"Contribution class not found: {contribution.ClassName}";
                    contributionHasError = true;
                }
                else
                {
                    contribution.ContributionHealthCheck = ModuleHealthCheckEnum.Success;
                }

                // Check tags in thesaurus
                bool preferredTagFound = false;

                foreach (string tag in contribution.Tags)
                {
                    (int ConceptId, string MainTerm)? concept = await _thesaurusService.GetConceptMainTermByTerm("Espluque", tag);

                    if (concept is not null && string.Equals(concept.Value.MainTerm, tag, StringComparison.OrdinalIgnoreCase))
                    {
                        preferredTagFound = true;
                        break;
                    }
                }

                if (!preferredTagFound)
                {
                    string tagDescription = contribution.Tags.Count == 0 ? "No tag is defined for this contribution." : $"No tag is defined as a preferred thesaurus term: {string.Join(", ", contribution.Tags)}";

                    contribution.ErrorDescription = string.IsNullOrWhiteSpace(contribution.ErrorDescription) ? tagDescription : $"{contribution.ErrorDescription}{Environment.NewLine}{tagDescription}";
                }
            }

            moduleDiagnostic.ModuleHealthCheck = contributionHasError ? ModuleHealthCheckEnum.Error : ModuleHealthCheckEnum.Success;

            return moduleDiagnostic;
        }

    }
}
