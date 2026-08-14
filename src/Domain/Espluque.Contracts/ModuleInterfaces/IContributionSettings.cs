namespace Espluque.Contracts.ModuleInterfaces
{
    public interface IContributionSettings
    {
        bool Active { get; set; }
        List<string> Tags { get; set; }
    }
}