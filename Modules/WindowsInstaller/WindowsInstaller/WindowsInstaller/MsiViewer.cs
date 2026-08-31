using Espluque.Contracts.Contributions.Types;
using Espluque.Contracts.CrossCutting;
using Espluque.Contracts.Workflow;
using System.IO;

namespace WindowsInstaller
{
    public class MsiViewer: IWpfViewer
    {
        private readonly IMessageCenter _messageCenter;
        private readonly ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        public MsiViewer(IMessageCenter messageCenter,
            ILogger logger,
            ISettingsService settingsService,
            IEntityFactory entityFactory)
        {
            _messageCenter = messageCenter;
            _logger = logger;
            _settingsService = settingsService;
            _entityFactory = entityFactory;
        }

        public MsiViewer()
        {

        }

        public async Task<object?> GetViewer(IAnalysisContext analysisContext)
        {
            object viewer = new MsiDbViewer(analysisContext.FilePath);
            return viewer;
        }
    }
}
