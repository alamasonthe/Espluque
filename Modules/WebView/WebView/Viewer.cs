using Espluque.Contracts.Contributions.Types;
using Espluque.Contracts.CrossCutting;
using Espluque.Contracts.Workflow;

namespace WebView
{
    public class Viewer: IWpfViewer
    {
        private readonly IMessageCenter _messageCenter;
        private readonly ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        public Viewer(IMessageCenter messageCenter,
            ILogger logger,
            ISettingsService settingsService,
            IEntityFactory entityFactory)
        {
            _messageCenter = messageCenter;
            _logger = logger;
            _settingsService = settingsService;
            _entityFactory = entityFactory;
        }

        public async Task<object?> GetViewer(IAnalysisContext analysisContext)
        {
            object viewer = new WebView2ViewerUC(analysisContext.FilePath);
            return viewer;
        }
    }

}
