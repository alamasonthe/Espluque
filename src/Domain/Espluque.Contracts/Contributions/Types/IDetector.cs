using Espluque.Contracts.Workflow;

namespace Espluque.Contracts.Contributions.Types
{
    public interface IDetector
    {
        Task<IFileFormat> Detect(IAnalysisContext analysisContext);
    }
}
