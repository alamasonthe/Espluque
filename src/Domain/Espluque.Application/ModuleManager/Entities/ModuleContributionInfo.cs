using Espluque.Contracts.ModuleInterfaces;

namespace Espluque.Application.ModuleManager.Entities
{
    public class ModuleContributionInfo : IModuleContributionInfo
    {
        public string InterfaceType { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;

        public IContributionSettings ContributionSettings { get; set; } = new ContributionSettings();
    }
}