using Espluque.Contracts.Workflow;

namespace Espluque.Contracts.Contributions.Types
{
    public interface IFusioner
    {
        Task<IAssertion> Fuse(IAnalysisContext analysisContext);
    }
}
