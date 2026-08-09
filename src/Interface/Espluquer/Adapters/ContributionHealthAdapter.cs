using Espluque.Contracts.Interfaces;
using Espluque.Contracts.ModuleInterfaces;
using Espluquer.Entities;

namespace Espluquer.Adapters
{
    internal class ContributionHealthAdapter
    {
        public static ContributionHealthDto FromDomain(IContributionHealth contributionHealth)
        {
            return new ContributionHealthDto
            {
                ModuleName = contributionHealth.ModuleName,
                ContribInterfaceType = contributionHealth.ContribInterfaceType,
                ContribClassName = contributionHealth.ContribClassName,
                HealthCheck = contributionHealth.HealthCheck,
                Diag = contributionHealth.Diag
            };
        }

        public static IContributionHealth ToDomain(
            ContributionHealthDto contributionHealthDto,
            IEntityFactory entityFactory)
        {
            return entityFactory.CreateContributionHealth(
                contributionHealthDto.ModuleName,
                contributionHealthDto.ContribInterfaceType,
                contributionHealthDto.ContribClassName,
                contributionHealthDto.HealthCheck,
                contributionHealthDto.Diag);
        }
    }
}