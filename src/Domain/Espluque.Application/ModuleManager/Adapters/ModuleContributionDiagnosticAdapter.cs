using Espluque.Application.ModuleManager.Entities;
using Espluque.Contracts.ModuleInterfaces;

namespace Espluque.Application.ModuleManager.Adapters
{
    public static class ModuleContributionDiagnosticAdapter
    {
        public static IModuleContributionDiagnostic ToDiagnostic(
            IModuleContributionInfo contributionInfo)
        {
            return new ModuleContributionDiagnostic
            {
                InterfaceType = contributionInfo.InterfaceType,
                Label = contributionInfo.Label,
                ClassName = contributionInfo.ClassName,
                Tags = [.. contributionInfo.Tags],
                Active = contributionInfo.Active
            };
        }
    }
}