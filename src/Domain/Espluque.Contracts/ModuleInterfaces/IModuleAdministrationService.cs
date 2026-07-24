namespace Espluque.Contracts.ModuleInterfaces
{
    public interface IModuleAdministrationService
    {
        Task<(string label, object instance)?> CreateAdminInstance(ICatalogEntry entry);
    }
}