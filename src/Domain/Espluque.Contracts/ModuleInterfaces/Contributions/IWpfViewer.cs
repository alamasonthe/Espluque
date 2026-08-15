using Espluque.Contracts.Interfaces;

namespace Espluque.Contracts.ModuleInterfaces.Contributions
{
    public interface IWpfViewer
    {
        Task<object?> GetViewer(IAnalysisContext analysisContext);
    }
}
