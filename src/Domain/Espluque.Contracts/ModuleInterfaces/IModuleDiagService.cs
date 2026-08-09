namespace Espluque.Contracts.ModuleInterfaces
{
    public interface IModuleDiagService
    {
        Task<(IModuleHealth ModuleHealth, List<IContributionHealth> ContributionHealths)> DiagAsync(string filePath);
    }
}