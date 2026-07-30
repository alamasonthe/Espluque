namespace Espluque.Contracts.Result
{
    public interface IGrabberResult
    {
        string ContributionLabel { get; set; }
        List<KeyValuePair<string, string>> GrabbedInformation { get; set; }
        string ModuleName { get; set; }
    }
}