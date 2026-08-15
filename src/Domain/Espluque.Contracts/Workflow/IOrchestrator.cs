using Espluque.Contracts.Interfaces;
using Espluque.Contracts.ModuleInterfaces;

namespace Espluque.Contracts.Workflow
{
    public interface IOrchestrator
    {
        event Action<IAnalysisMessage>? AnalyserMessageEvent;

        Task<IAnalysisContext> ProcessAsync(List<ICatalogEntry> catalog, IAnalysisContext analysisContext, string? viewerType);
    }
}