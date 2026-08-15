namespace Espluque.Contracts.Workflow
{
    public interface IAnalysisEngine
    {
        event Action<IAnalysisMessage>? AnalyserMessageEvent;

        Task<IAnalysisContext> AnalyzeFileAsync(IAnalysisContext analysisContext, string? viewerType = null);
    }
}