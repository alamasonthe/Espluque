using Espluque.Contracts.CrossCutting;
using Espluque.Contracts.Modules;

namespace Espluque.Application.Modules
{
    public class ModuleHealth : IModuleHealth
    {
        public string ModuleName { get; set; } = string.Empty;
        public ModuleHealthCheckEnum HealthCheck { get; set; } = ModuleHealthCheckEnum.NotTested;

        public string? Diag { get; set; }
    }
}
