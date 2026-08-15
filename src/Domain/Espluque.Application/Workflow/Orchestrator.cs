using Espluque.Application.CrossCutting;
using Espluque.Contracts.Detection;
using Espluque.Contracts.Enums;
using Espluque.Contracts.Interfaces;
using Espluque.Contracts.ModuleInterfaces;
using Espluque.Contracts.Ports;
using Espluque.Contracts.Workflow;
using Microsoft.Extensions.DependencyInjection;

namespace Espluque.Application.Workflow
{
    /// <summary>
    /// Coordinates the analysis workflow by executing the analysis engine followed by the fusion engine.
    /// </summary>
    /// <remarks>
    /// Processing cycle:
    /// <code>
    /// AnalysisContext
    ///     ↓
    /// Execute AnalysisEngine
    ///     ↓
    /// Execute FusionEngine
    ///     ↓
    /// Emit AnalysisCompleted message
    ///     ↓
    /// Return updated AnalysisContext
    /// </code>
    ///
    /// Messages emitted by the analysis and fusion engines are relayed through AnalyserMessageEvent.
    ///
    /// The optional viewerType parameter is forwarded to AnalysisEngine to restrict viewer contribution execution.
    /// </remarks>

    public class Orchestrator : IOrchestrator
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Espluque.Contracts.Ports.ILogger _logger;
        private readonly ISettingsService _settingsService;

        private readonly IAnalysisEngine _analysisEngine;

        public event Action<IAnalysisMessage>? AnalyserMessageEvent;

        public Orchestrator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            _logger = serviceProvider.GetRequiredService<Espluque.Contracts.Ports.ILogger>();
            _settingsService = serviceProvider.GetRequiredService<ISettingsService>();
        }
        public async Task<IAnalysisContext> ProcessAsync(List<ICatalogEntry> catalog, IAnalysisContext analysisContext, string? viewerType)
        {
            var formattedFilename = FormattedFileName(analysisContext);
            AnalysisEngine analysisEngine = new AnalysisEngine(_serviceProvider, catalog);
            analysisEngine.AnalyserMessageEvent += RelayAnalyserMessage;

            FusionEngine fusionEngine = new FusionEngine(_serviceProvider, catalog);
            fusionEngine.AnalyserMessageEvent += RelayAnalyserMessage;

            try
            {
                var context = await analysisEngine.AnalyzeFileAsync(analysisContext, viewerType);

                context = await fusionEngine.FuseAnalysis(context);

                IAnalysisMessage message = new Factory().CreateAnalysisMessage( AnalysisMessageTypeEnum.AnalysisCompleted, true, null, null, null, null);

                AnalyserMessageEvent?.Invoke(message);

                return context;
            }
            finally
            {
                analysisEngine.AnalyserMessageEvent -= RelayAnalyserMessage;
                fusionEngine.AnalyserMessageEvent -= RelayAnalyserMessage;
            }

        }

        private void RelayAnalyserMessage(IAnalysisMessage message)
        {
            AnalyserMessageEvent?.Invoke(message);
        }

        private string FormattedFileName(IAnalysisContext analysisContext)
        {
            return Path.GetFileName(analysisContext.FilePath).PadRight(35);
        }
    }
}
