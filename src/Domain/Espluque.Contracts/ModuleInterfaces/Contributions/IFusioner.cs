using Espluque.Contracts.Interfaces;

namespace Espluque.Contracts.ModuleInterfaces.Contributions
{
    public interface IFusioner
    {
        Task<IAssertion> Fuse(IAnalysisContext analysisContext);
    }
}
