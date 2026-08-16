using Espluque.Contracts.Contributions;

namespace Espluque.Contracts.Modules
{
    public interface IModuleDiagService
    {
        Task<(IModuleHealth ModuleHealth, List<IContributionHealth> ContributionHealths)> DiagAsync(string filePath);
    }
}