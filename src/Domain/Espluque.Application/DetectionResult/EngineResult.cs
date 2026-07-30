using Espluque.Contracts.Entities;
using Espluque.Contracts.DetectionResult;

namespace Espluque.Application.DetectionResult
{
    public class EngineResult : IEngineResult
    {
        public AnalysisContext AnalysisContext { get; set; }

        public List<IGrabberResult> GrabberResults { get; set; } = [];
    }
}
