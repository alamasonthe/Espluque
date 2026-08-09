using Espluque.Contracts.Enums;
using Espluque.Contracts.ModuleInterfaces;

namespace Espluque.Application.ModuleManager.Entities
{
    public class ContributionHealth : IContributionHealth
    {
        public string ModuleName { get; set; } = string.Empty;
        public string ContribInterfaceType { get; set; } = string.Empty;
        public string ContribClassName { get; set; } = string.Empty;

        public ModuleHealthCheckEnum HealthCheck { get; set; } = ModuleHealthCheckEnum.NotTested;

        public string? Diag { get; set; }
    }
}
