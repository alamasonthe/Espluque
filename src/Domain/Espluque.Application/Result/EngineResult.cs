using Espluque.Contracts.Entities;
using Espluque.Contracts.Result;

namespace Espluque.Application.Result
{
    public class EngineResult : IEngineResult
    {
        public AnalysisContext AnalysisContext { get; set; }

        public List<IGrabberResult> GrabberResults { get; set; } = [];
    }
}
