using Espluque.Contracts.Entities;

namespace Espluque.Contracts.DetectionResult
{
    public interface IEngineResult
    {
        
        List<IGrabberResult> GrabberResults { get; set; }
    }
}