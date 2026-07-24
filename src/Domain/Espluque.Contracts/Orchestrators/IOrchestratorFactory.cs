using Espluque.Contracts.Ports;

namespace Espluque.Contracts.Orchestrators
{
    public interface IOrchestratorFactory
    {
        IEngine CreateEngine();
    }
}