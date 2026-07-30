using Espluque.Application.Detection;
using Espluque.Contracts.Detection;
using Espluque.Contracts.DetectionResult;
using Espluque.Contracts.Entities;
using Espluque.Contracts.Interfaces;
using Espluque.Contracts.ModuleInterfaces;
using Espluque.Contracts.Ports;
using Espluque.Contracts.Workflow;
using Microsoft.Extensions.DependencyInjection;
using Espluque.Application.DetectionResult;

namespace Espluque.Application.Workflow
{
    public class Orchestrator : IOrchestrator
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Espluque.Contracts.Ports.ILogger _logger;
        private readonly ISettingsService _settingsService;

        private readonly IEngine _engine;

        public event Action<IAnalysisMessage>? AnalyserMessageEvent;

        public Orchestrator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            _logger = serviceProvider.GetRequiredService<Espluque.Contracts.Ports.ILogger>();
            _settingsService = serviceProvider.GetRequiredService<ISettingsService>();
        }
        public async Task<IEngineResult> ProcessAsync(List<ICatalogEntry> catalog, AnalysisContext analysisContext)
        {

            IEngine engine = new Engine(_serviceProvider, catalog);

            engine.AnalyserMessageEvent += RelayAnalyserMessage;

            try
            {
                await engine.AnalyzeFileAsync(analysisContext);


                IEngineResult result = new EngineResult
                {
                    AnalysisContext = analysisContext,
                    GrabberResults = new List<IGrabberResult>()
                };

                return result;
            }
            finally
            {
                engine.AnalyserMessageEvent -= RelayAnalyserMessage;
            }

        }

        private void RelayAnalyserMessage(IAnalysisMessage message)
        {
            AnalyserMessageEvent?.Invoke(message);
        }
    }
}
