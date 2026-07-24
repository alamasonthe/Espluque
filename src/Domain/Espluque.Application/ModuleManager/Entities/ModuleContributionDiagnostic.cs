using Espluque.Contracts.ModuleInterfaces;
using Espluque.Contracts.Enums;

namespace Espluque.Application.ModuleManager.Entities
{
    public class ModuleContributionDiagnostic : IModuleContributionDiagnostic
    {
        public string InterfaceType { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public string ClassName { get; set; } = string.Empty;

        public List<string> Tags { get; set; } = new();

        public bool Active { get; set; } = true;

        public ModuleHealthCheckEnum ContributionHealthCheck { get; set; } = ModuleHealthCheckEnum.NotTested;

        public string? ErrorDescription { get; set; }
    }
}
