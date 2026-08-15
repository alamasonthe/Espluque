namespace Espluque.Contracts.Contributions
{
    public interface IGrabberResult
    {
        string ModuleName { get; set; }
        string ContributionLabel { get; set; }
        List<KeyValuePair<string, string>> GrabbedInformation { get; set; }
    }
}