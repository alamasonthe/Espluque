using Espluque.Contracts.Contributions.Types;
using Espluque.Contracts.CrossCutting;
using Espluque.Contracts.Workflow;
using System.IO;

namespace HexaEditor
{
    public class Viewer : IWpfViewer, IManagedDependencies
    {
        private readonly IMessageCenter _messageCenter;
        private readonly ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        public Viewer()
        {

        }

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
            object viewer = new HexEditUC(analysisContext.FilePath);
            return viewer;
        }

        public List<string> GetDependencyPaths()
        {
            string moduleRootPath = Path.GetDirectoryName(typeof(Viewer).Assembly.Location)!;

            List<string> paths =
            [
                Path.Combine(moduleRootPath, "WPFHexaEditor.dll")
            ];

            return paths;
        }
    }

}
