using Espluque.Contracts.Catalog;

namespace Espluque.Contracts.Modules
{
    public interface IModuleAdministrationService
    {
        Task<(string label, object instance)?> CreateAdminInstance(ICatalogEntry entry);
    }
}