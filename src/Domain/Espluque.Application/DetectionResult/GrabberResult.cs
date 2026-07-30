using Espluque.Contracts.DetectionResult;

namespace Espluque.Application.DetectionResult
{
    public class GrabberResult : IGrabberResult
    {
        public string ModuleName { get; set; }
        public string ContributionLabel { get; set; }
        public List<KeyValuePair<string, string>> GrabbedInformation { get; set; } = [];
    }
}
