using Espluque.Contracts.Result;

namespace Espluque.Application.Result
{
    public class GrabberResult : IGrabberResult
    {
        public string ModuleName { get; set; }
        public string ContributionLabel { get; set; }
        public List<KeyValuePair<string, string>> GrabbedInformation { get; set; } = [];
    }
}
