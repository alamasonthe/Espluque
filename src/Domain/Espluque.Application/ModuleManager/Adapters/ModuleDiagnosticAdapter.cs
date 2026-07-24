using Espluque.Application.ModuleManager.Entities;
using Espluque.Contracts.ModuleInterfaces;

namespace Espluque.Application.ModuleManager.Adapters
{
    public static class ModuleDiagnosticAdapter
    {
        public static IModuleDiagnostic ToDiagnostic(
            IModuleInfo moduleInfo,
            string filePath,
            string json)
        {
            return new ModuleDiagnostic
            {
                FilePath = filePath,
                Json = json,
                Name = moduleInfo.Name,
                Version = moduleInfo.Version,
                Assembly = moduleInfo.Assembly,
                Contributions = moduleInfo.Contributions
                    .Select(ModuleContributionDiagnosticAdapter.ToDiagnostic)
                    .ToList()
            };
        }
    }
}