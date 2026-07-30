namespace Espluque.Contracts.DetectionResult
{
    public record ResultPropertyLink(
        string GrabberModuleName,
        string GrabberContributionLabel,
        string GrabberKey,
        string ResultModelPropertyName);
}
