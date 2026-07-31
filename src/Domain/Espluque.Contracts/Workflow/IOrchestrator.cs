using Espluque.Contracts.DetectionResult;
using Espluque.Contracts.Entities;
using Espluque.Contracts.Interfaces;
using Espluque.Contracts.ModuleInterfaces;

namespace Espluque.Contracts.Workflow
{
    public interface IOrchestrator
    {
        event Action<IAnalysisMessage>? AnalyserMessageEvent;

        Task<IEngineResult> ProcessAsync(List<ICatalogEntry> catalog, AnalysisContext analysisContext, string? viewerType);
    }
}