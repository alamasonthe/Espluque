using Espluque.Contracts.Enums;
using Espluque.Contracts.ModuleInterfaces;

namespace Espluque.Application.Contributions
{
    /// <summary>
    /// Represents the diagnostic state of a module contribution.
    /// </summary>
    /// <remarks>
    /// Identifies the contribution by module, interface type and class name.
    /// HealthCheck contains the diagnostic result; Diag contains details from class and tag validation.
    /// </remarks>

    public class ContributionHealth : IContributionHealth
    {
        public string ModuleName { get; set; } = string.Empty;
        public string ContribInterfaceType { get; set; } = string.Empty;
        public string ContribClassName { get; set; } = string.Empty;

        public ModuleHealthCheckEnum HealthCheck { get; set; } = ModuleHealthCheckEnum.NotTested;

        public string? Diag { get; set; }
    }
}
