using Espluque.Contracts.Catalog;

namespace Espluque.Contracts.Workflow
{
    public interface IOrchestrator
    {
        event Action<IAnalysisMessage>? AnalyserMessageEvent;

        Task<IAnalysisContext> ProcessAsync(List<ICatalogEntry> catalog, IAnalysisContext analysisContext, string? viewerType);
    }
}