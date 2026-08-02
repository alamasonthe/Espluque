using Espluque.Contracts.Entities;

namespace Espluque.Contracts.ModuleInterfaces.Contributions
{
    public interface IFusioner
    {
        Task<IAssertion> Fuse(AnalysisContext analysisContext);
    }
}
