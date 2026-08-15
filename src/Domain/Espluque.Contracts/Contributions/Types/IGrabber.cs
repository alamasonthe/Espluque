using Espluque.Contracts.Workflow;

namespace Espluque.Contracts.Contributions.Types
{
    public interface IGrabber
    {
        Task<List<KeyValuePair<string, string>>> Grab(IAnalysisContext analysisContext);
    }
}
