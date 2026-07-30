using Espluque.Contracts.Entities;
using Espluque.Contracts.Interfaces;

namespace Espluque.Contracts.Detection
{
    public interface IEngine
    {
        event Action<IAnalysisMessage>? AnalyserMessageEvent;

        Task AnalyzeFileAsync(AnalysisContext analysisContext);
    }
}