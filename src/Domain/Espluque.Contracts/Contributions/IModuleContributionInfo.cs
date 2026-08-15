namespace Espluque.Contracts.Contributions
{
    public interface IModuleContributionInfo
    {
        string ClassName { get; set; }
        string InterfaceType { get; set; }
        string Label { get; set; }
        IContributionSettings ContributionSettings { get; set; }
    }
}