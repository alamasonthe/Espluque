using Espluque.Contracts.Detection;
using Microsoft.Extensions.DependencyInjection;

namespace Espluque.Application.Detection
{
    public sealed class EngineFactory : IEngineFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public EngineFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IEngine CreateEngine()
        {
            return _serviceProvider.GetRequiredService<Engine>();
        }
    }
}