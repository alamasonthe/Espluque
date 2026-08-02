using Espluque.Contracts.Entities;

using Espluque.Contracts.Interfaces;

namespace Espluque.Contracts.Detection
{
    public interface IAnalysisEngine
    {
        event Action<IAnalysisMessage>? AnalyserMessageEvent;

        Task<AnalysisContext> AnalyzeFileAsync(AnalysisContext analysisContext, string? viewerType = null);
    }
}