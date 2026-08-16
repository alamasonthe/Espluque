namespace Espluque.Contracts.Contributions
{
    public interface IContributionSettingsEntry
    {
        string ClassName { get; set; }
        string InterfaceType { get; set; }
        string ModuleAssembly { get; set; }
        IContributionSettings Settings { get; set; }
    }
}