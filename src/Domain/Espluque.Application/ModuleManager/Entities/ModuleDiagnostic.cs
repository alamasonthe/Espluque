using Espluque.Contracts.Enums;
using Espluque.Contracts.ModuleInterfaces;

namespace Espluque.Application.ModuleManager.Entities
{
    public class ModuleDiagnostic: IModuleDiagnostic
    {
        public string FilePath { get; set; } = string.Empty;
        public string Json { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Assembly { get; set; } = string.Empty;
        public List<IModuleContributionDiagnostic> Contributions { get; set; } = [];
        public ModuleHealthCheckEnum ModuleHealthCheck { get; set; } = ModuleHealthCheckEnum.NotTested;
        public string? ErrorDescription { get; set; }
    }
}
