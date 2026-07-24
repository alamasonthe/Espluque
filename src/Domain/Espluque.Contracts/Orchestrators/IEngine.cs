using Espluque.Contracts.Interfaces;

namespace Espluque.Contracts.Orchestrators
{
    public interface IEngine
    {
        event Action<IAnalysisMessage>? AnalyserMessageEvent;

        Task AnalyzeFileAsync( string filePath);
    }
}