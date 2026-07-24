namespace Espluque.Contracts.ModuleInterfaces
{
    public interface IModuleContributionInfo
    {
        bool Active { get; set; }
        string ClassName { get; set; }
        string InterfaceType { get; set; }
        string Label { get; set; }
        List<string> Tags { get; set; }
    }
}