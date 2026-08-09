namespace Espluque.Contracts.ModuleInterfaces
{
    public interface IModuleInfo
    {
        string Author { get; set; }
        string Description { get; set; }
        string FilePath { get; set; }
        string Assembly { get; set; }
        List<IModuleContributionInfo> Contributions { get; set; }
        string Name { get; set; }
        string Version { get; set; }
    }
}