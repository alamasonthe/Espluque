using Espluque.Contracts.Entities;

namespace Espluque.Contracts.ModuleInterfaces.Contributions
{
    public interface IGrabber
    {
        Task<List<KeyValuePair<string, string>>> Grab(AnalysisContext analysisContext);
    }
}
