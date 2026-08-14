using Espluque.Contracts.ModuleInterfaces;

namespace Espluque.Application.ModuleManager.Entities
{
    public class ContributionSettingsEntry : IContributionSettingsEntry
    {
        public string ModuleAssembly { get; set; } = string.Empty;
        public string InterfaceType { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;

        public IContributionSettings Settings { get; set; }
    }
}
