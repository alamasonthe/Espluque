using Espluque.Contracts.Workflow;

namespace Espluque.Contracts.Contributions.Types
{
    public interface IWpfViewer
    {
        Task<object?> GetViewer(IAnalysisContext analysisContext);
    }
}
