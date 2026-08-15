using Espluque.Contracts.CrossCutting;

namespace Espluque.Contracts.Modules
{
    public interface IModuleHealth
    {
        string? Diag { get; set; }
        ModuleHealthCheckEnum HealthCheck { get; set; }
        string ModuleName { get; set; }
    }
}