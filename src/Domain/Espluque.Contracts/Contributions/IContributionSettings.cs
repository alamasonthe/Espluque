namespace Espluque.Contracts.Contributions
{
    public interface IContributionSettings
    {
        bool Active { get; set; }
        List<string> Tags { get; set; }
    }
}