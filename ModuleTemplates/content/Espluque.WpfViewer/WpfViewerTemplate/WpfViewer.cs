using Espluque.Contracts.CrossCutting;
using Espluque.Contracts.Contributions.Types;
using Espluque.Contracts.Workflow;

namespace WpfViewerTemplate
{
    public class WpfViewer: IWpfViewer
    {
        private readonly IMessageCenter _messageCenter;
        private readonly ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        public WpfViewer()
        {

        }

        public WpfViewer(IMessageCenter messageCenter,
            ILogger logger,
            ISettingsService settingsService,
            IEntityFactory entityFactory)
        {
            _messageCenter = messageCenter;
            _logger = logger;
            _settingsService = settingsService;
            _entityFactory = entityFactory;
        }

        public Task<object?> GetViewer(IAnalysisContext analysisContext)
        {
            object viewer = new CustomUserControl(analysisContext.FilePath);
            return Task.FromResult<object?>(viewer);
        }
    }

}
