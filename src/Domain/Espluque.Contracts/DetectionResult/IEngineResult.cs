using Espluque.Contracts.Entities;

namespace Espluque.Contracts.DetectionResult
{
    public interface IEngineResult
    {
        AnalysisContext AnalysisContext { get; set; }
        List<IGrabberResult> GrabberResults { get; set; }
    }
}