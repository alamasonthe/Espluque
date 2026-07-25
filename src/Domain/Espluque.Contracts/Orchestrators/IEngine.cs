using Espluque.Contracts.Entities;
using Espluque.Contracts.Interfaces;

namespace Espluque.Contracts.Orchestrators
{
    public interface IEngine
    {
        event Action<IAnalysisMessage>? AnalyserMessageEvent;

        Task AnalyzeFileAsync(AnalysisContext analysisContext);
    }
}