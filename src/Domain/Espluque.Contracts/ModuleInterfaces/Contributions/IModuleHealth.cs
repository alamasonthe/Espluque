using Espluque.Contracts.Enums;

namespace Espluque.Contracts.ModuleInterfaces
{
    public interface IModuleHealth
    {
        string? Diag { get; set; }
        ModuleHealthCheckEnum HealthCheck { get; set; }
        string ModuleName { get; set; }
    }
}