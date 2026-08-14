namespace Espluque.Contracts.ModuleInterfaces
{
    public interface IContributionSettingsEntry
    {
        string ClassName { get; set; }
        string InterfaceType { get; set; }
        string ModuleAssembly { get; set; }
        IContributionSettings Settings { get; set; }
    }
}