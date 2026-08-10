using Espluque.Contracts.ModuleInterfaces;

namespace Espluque.Application.ModuleManager.Entities
{
    public class ContributionSettings : IContributionSettings
    {
        public List<string> Tags { get; set; } = new();

        public bool Active { get; set; } = true;
    }
}
