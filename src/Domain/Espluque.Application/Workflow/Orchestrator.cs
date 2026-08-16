using Espluque.Application.CrossCutting;
using Espluque.Contracts.Catalog;
using Espluque.Contracts.CrossCutting;
using Espluque.Contracts.Thesaurus;
using Espluque.Contracts.Workflow;

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
        private readonly IMessageCenter _messageCenter;
        private readonly ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;
        private readonly IThesaurusService _thesaurusService;

        public event Action<IAnalysisMessage>? AnalyserMessageEvent;

        public Orchestrator(
            IMessageCenter messageCenter,
            ILogger logger,
            ISettingsService settingsService,
            IEntityFactory entityFactory,
            IThesaurusService thesaurusService)
        {
            _messageCenter = messageCenter;
            _logger = logger;
            _settingsService = settingsService;
            _entityFactory = entityFactory;
            _thesaurusService = thesaurusService;
        }

        public async Task<IAnalysisContext> ProcessAsync(List<ICatalogEntry> catalog, IAnalysisContext analysisContext, string? viewerType)
        {
            var formattedFilename = FormattedFileName(analysisContext);
            AnalysisEngine analysisEngine = new AnalysisEngine(
                _messageCenter,
                _logger,
                _settingsService,
                _entityFactory,
                _thesaurusService,
                catalog);

            analysisEngine.AnalyserMessageEvent += RelayAnalyserMessage;

            FusionEngine fusionEngine = new FusionEngine(
                _messageCenter,
                _logger,
                _settingsService,
                _entityFactory,
                _thesaurusService,
                catalog);

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
