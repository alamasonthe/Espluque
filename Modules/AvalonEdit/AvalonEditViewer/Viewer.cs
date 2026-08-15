using Espluque.Contracts.Interfaces;
using Espluque.Contracts.MessageInterfaces;
using Espluque.Contracts.ModuleInterfaces.Contributions;
using Espluque.Contracts.Ports;
using System.IO;

namespace AvalonEditViewer
{
    public class Viewer: IWpfViewer, IManagedDependencies
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

        public Viewer()
        {

        }

        public async Task<object?> GetViewer(IAnalysisContext analysisContext)
        {
            object viewer = new TextEditorUC(analysisContext.FilePath);
            return viewer;
        }

        public List<string> GetDependencyPaths()
        {
            string moduleRootPath = Path.GetDirectoryName(typeof(Viewer).Assembly.Location)!;

            List<string> paths =
            [
                Path.Combine(moduleRootPath, "ICSharpCode.AvalonEdit.dll")
            ];

            return paths;
        }
    }

}
