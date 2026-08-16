using Espluque.Contracts.Contributions;

namespace Espluque.Application.Contributions
{
    /// <summary>
    /// Represents the result produced by a grabber contribution during analysis.
    /// </summary>
    /// <remarks>
    /// The result identifies the module and contribution that produced the data
    /// and contains the information extracted by the grabber as key/value pairs.
    /// </remarks>

    public class GrabberResult : IGrabberResult
    {
        public string ModuleName { get; set; }
        public string ContributionLabel { get; set; }
        public List<KeyValuePair<string, string>> GrabbedInformation { get; set; } = [];
    }
}
