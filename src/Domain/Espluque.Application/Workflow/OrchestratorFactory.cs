using Espluque.Contracts.Workflow;
using Microsoft.Extensions.DependencyInjection;

namespace Espluque.Application.Workflow
{
    /// <summary>
    /// Creates IOrchestrator instances from the configured dependency injection container.
    /// </summary>
    /// <remarks>
    /// Orchestrator instances are resolved through IServiceProvider using the registered Orchestrator implementation.
    /// </remarks>

    public sealed class OrchestratorFactory : IOrchestratorFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public OrchestratorFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IOrchestrator CreateOrchestrator()
        {
            return _serviceProvider.GetRequiredService<Orchestrator>();
        }
    }
}
