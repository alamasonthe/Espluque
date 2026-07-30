using Espluque.Application.Detection;
using Espluque.Contracts.Workflow;
using Microsoft.Extensions.DependencyInjection;

namespace Espluque.Application.Workflow
{
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
