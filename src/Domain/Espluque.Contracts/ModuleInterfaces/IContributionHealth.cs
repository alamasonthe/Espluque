using Espluque.Contracts.Enums;

namespace Espluque.Contracts.ModuleInterfaces
{
    public interface IContributionHealth
    {
        string ContribClassName { get; set; }
        string ContribInterfaceType { get; set; }
        string? ErrorDescription { get; set; }
        ModuleHealthCheckEnum HealthCheck { get; set; }
        string ModuleName { get; set; }
    }
}