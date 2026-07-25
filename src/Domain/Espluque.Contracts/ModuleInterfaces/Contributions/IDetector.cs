using Espluque.Contracts.Entities;
using Espluque.Contracts.Interfaces;

namespace Espluque.Contracts.ModuleInterfaces.Contributions
{
    public interface IDetector
    {
        Task<IFileFormat> Detect(AnalysisContext analysisContext);
    }
}
