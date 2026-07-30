using Espluque.Contracts.Detection;

namespace Espluque.Contracts.Workflow
{
    public interface IOrchestratorFactory
    {
        IOrchestrator CreateOrchestrator();
    }
}