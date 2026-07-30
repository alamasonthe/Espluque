using Espluque.Contracts.Entities;

namespace Espluque.Contracts.Result
{
    public interface IEngineResult
    {
        AnalysisContext AnalysisContext { get; set; }
        List<IGrabberResult> GrabberResults { get; set; }
    }
}