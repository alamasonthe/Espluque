using Espluque.Contracts.Interfaces;

namespace Espluque.Contracts.Detection
{
    public interface IAnalysisEngine
    {
        event Action<IAnalysisMessage>? AnalyserMessageEvent;

        Task<IAnalysisContext> AnalyzeFileAsync(IAnalysisContext analysisContext, string? viewerType = null);
    }
}