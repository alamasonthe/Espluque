using Espluque.Contracts.CrossCutting;

namespace Espluque.Contracts.Contributions
{
    public interface IContributionHealth
    {
        string ContribClassName { get; set; }
        string ContribInterfaceType { get; set; }
        string? Diag { get; set; }
        ModuleHealthCheckEnum HealthCheck { get; set; }
        string ModuleName { get; set; }
    }
}