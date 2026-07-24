using Espluque.Contracts.Orchestrators;
using Microsoft.Extensions.DependencyInjection;

namespace Espluque.Application.Orchestrators
{
    public sealed class OrchestratorFactory : IOrchestratorFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public OrchestratorFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IEngine CreateEngine()
        {
            return _serviceProvider.GetRequiredService<Engine>();
        }
    }
}