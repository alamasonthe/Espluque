using Espluque.Contracts.Contributions.Types;
using Espluque.Contracts.CrossCutting;
using Espluque.Contracts.Workflow;

namespace WindowsInstaller
{
    public class MsiFileViewer : IWpfViewer
    {
        private readonly IMessageCenter _messageCenter;
        private readonly ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        public MsiFileViewer(IMessageCenter messageCenter,
            ILogger logger,
            ISettingsService settingsService,
            IEntityFactory entityFactory)
        {
            _messageCenter = messageCenter;
            _logger = logger;
            _settingsService = settingsService;
            _entityFactory = entityFactory;
        }

        public MsiFileViewer()
        {

        }

        public async Task<object?> GetViewer(IAnalysisContext analysisContext)
        {
            object viewer = new MsiFileViewerUC(
                analysisContext.FilePath!,
                analysisContext.TempFolderPath!,
                _messageCenter,
                _entityFactory,
                _logger);

            return viewer;
        }
    }
}
