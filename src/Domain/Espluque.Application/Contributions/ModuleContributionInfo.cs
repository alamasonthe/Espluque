using Espluque.Contracts.ModuleInterfaces;

namespace Espluque.Application.Contributions
{
    /// <summary>
    /// Describes a contribution declared in a module definition.
    /// </summary>
    /// <remarks>
    /// Used when reading the contribution entries contained in a module JSON descriptor represented by IModuleInfo.
    /// ContributionSettings carries the activation state and thesaurus tags defined for that contribution.
    /// </remarks>

    public class ModuleContributionInfo : IModuleContributionInfo
    {
        public string InterfaceType { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;

        public IContributionSettings ContributionSettings { get; set; } = new ContributionSettings();
    }
}