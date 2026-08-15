using Espluque.Contracts.Interfaces;

namespace Espluque.Contracts.ModuleInterfaces.Contributions
{
    public interface IGrabber
    {
        Task<List<KeyValuePair<string, string>>> Grab(IAnalysisContext analysisContext);
    }
}
