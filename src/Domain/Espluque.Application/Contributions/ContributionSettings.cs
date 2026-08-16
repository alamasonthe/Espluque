using Espluque.Contracts.Contributions;

namespace Espluque.Application.Contributions
{
    /// <summary>
    /// Defines the runtime configuration of a contribution.
    /// </summary>
    /// <remarks>
    /// Active controls whether the contribution is included in the runtime catalog.
    /// Tags associate the contribution with thesaurus terms used for its selection during analysis.
    /// </remarks>

    public class ContributionSettings : IContributionSettings
    {
        public List<string> Tags { get; set; } = new();

        public bool Active { get; set; } = true;
    }
}
