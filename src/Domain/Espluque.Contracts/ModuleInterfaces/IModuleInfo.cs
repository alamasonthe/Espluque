namespace Espluque.Contracts.ModuleInterfaces
{
    public interface IModuleInfo
    {
        string Assembly { get; set; }
        List<IModuleContributionInfo> Contributions { get; set; }
        string Name { get; set; }
        string Version { get; set; }
    }
}