namespace Espluque.Contracts.Result
{
    public record ResultPropertyLink(
        string GrabberModuleName,
        string GrabberContributionLabel,
        string GrabberKey,
        string ResultModelPropertyName);
}
