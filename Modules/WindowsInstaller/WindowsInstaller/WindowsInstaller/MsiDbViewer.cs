using Espluque.Contracts.Contributions.Types;
using Espluque.Contracts.CrossCutting;
using Espluque.Contracts.Workflow;
using System.IO;

namespace WindowsInstaller
{
    public class MsiDbViewer: IWpfViewer
    {
        private readonly IMessageCenter _messageCenter;
        private readonly ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        public MsiDbViewer(IMessageCenter messageCenter,
            ILogger logger,
            ISettingsService settingsService,
            IEntityFactory entityFactory)
        {
            _messageCenter = messageCenter;
            _logger = logger;
            _settingsService = settingsService;
            _entityFactory = entityFactory;
        }

        public MsiDbViewer()
        {

        }

        public async Task<object?> GetViewer(IAnalysisContext analysisContext)
        {
            object viewer = new MsiDbViewerUC(analysisContext.FilePath);
            return viewer;
        }
    }
}
