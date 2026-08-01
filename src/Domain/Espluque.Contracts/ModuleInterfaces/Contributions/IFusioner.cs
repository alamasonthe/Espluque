using Espluque.Contracts.Entities;

namespace Espluque.Contracts.ModuleInterfaces.Contributions
{
    public interface IFusioner
    {
        Task<List<KeyValuePair<string, string>>> Fuse(AnalysisContext analysisContext);
    }
}
