using Espluque.Contracts.Interfaces;
using Espluque.Contracts.ModuleInterfaces;
using Espluquer.Entities;

namespace Espluquer.Adapters
{
    internal class ModuleHealthAdapter
    {
        public static ModuleHealthDto FromDomain(IModuleHealth moduleHealth)
        {
            return new ModuleHealthDto
            {
                ModuleName = moduleHealth.ModuleName,
                HealthCheck = moduleHealth.HealthCheck,
                Diag = moduleHealth.Diag
            };
        }

        public static IModuleHealth ToDomain(
            ModuleHealthDto moduleHealthDto,
            IEntityFactory entityFactory)
        {
            return entityFactory.CreateModuleHealth(
                moduleHealthDto.ModuleName,
                moduleHealthDto.HealthCheck,
                moduleHealthDto.Diag);
        }
    }
}