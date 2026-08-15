using Espluque.Contracts.ModuleInterfaces;

namespace Espluque.Application.Contributions
{
    /// <summary>
    /// Associates persisted settings with a specific contribution.
    /// </summary>
    /// <remarks>
    /// ModuleAssembly, InterfaceType and ClassName identify the contribution.
    /// Settings contains its active state and associated tags.
    /// </remarks>

    public class ContributionSettingsEntry : IContributionSettingsEntry
    {
        public string ModuleAssembly { get; set; } = string.Empty;
        public string InterfaceType { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;

        public IContributionSettings Settings { get; set; }
    }
}
