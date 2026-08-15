namespace Espluque.Contracts.Workflow
{
    public interface IOrchestratorFactory
    {
        IOrchestrator CreateOrchestrator();
    }
}