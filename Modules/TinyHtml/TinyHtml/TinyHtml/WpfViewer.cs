using Espluque.Contracts.Contributions.Types;
using Espluque.Contracts.CrossCutting;
using Espluque.Contracts.Workflow;
using System.IO;

namespace TinyHtml
{
    public class WpfViewer : IWpfViewer, IManagedDependencies
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
            object viewer = new TinyHtmlUC(analysisContext.FilePath);
            return Task.FromResult<object?>(viewer);
        }

        public List<string> GetDependencyPaths()
        {
            string moduleRootPath = Path.GetDirectoryName(typeof(WpfViewer).Assembly.Location)!;

            List<string> paths =
            [
                Path.Combine(moduleRootPath, "TinyHtml.Wpf.dll")
            ];

            return paths;
        }
    }

}
