using Espluque.Contracts.Entities;

namespace Espluque.Contracts.ModuleInterfaces.Contributions
{
    public interface IWpfViewer
    {
        Task<object?> GetViewer(AnalysisContext analysisContext);
    }
}
