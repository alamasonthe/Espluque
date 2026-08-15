namespace Espluque.Contracts.Contributions
{
    public record ResultPropertyLink(
        string GrabberModuleName,
        string GrabberContributionLabel,
        string GrabberKey,
        string ResultModelPropertyName);
}
