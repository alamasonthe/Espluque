using Espluque.Contracts.Enums;

namespace Espluque.Contracts.ModuleInterfaces
{
    public interface IModuleContributionDiagnostic 
    {
        bool Active { get; set; }
        string ClassName { get; set; }
        string InterfaceType { get; set; }
        string Label { get; set; }
        List<string> Tags { get; set; }
        ModuleHealthCheckEnum ContributionHealthCheck { get; set; }
        string? ErrorDescription { get; set; }
    }
}