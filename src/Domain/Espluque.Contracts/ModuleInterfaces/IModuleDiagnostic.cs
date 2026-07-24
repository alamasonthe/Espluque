using Espluque.Contracts.Enums;

namespace Espluque.Contracts.ModuleInterfaces
{
    public interface IModuleDiagnostic
    {
        string FilePath { get; set; }
        string Json { get; set; }
        string Assembly { get; set; }
        List<IModuleContributionDiagnostic> Contributions { get; set; }
        string Name { get; set; }
        string Version { get; set; }
        ModuleHealthCheckEnum ModuleHealthCheck { get; set; }
        string? ErrorDescription { get; set; }
    }
}
