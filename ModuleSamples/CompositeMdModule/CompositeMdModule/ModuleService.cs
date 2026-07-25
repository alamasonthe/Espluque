using Espluque.Contracts.Entities;
using Espluque.Contracts.Interfaces;
using Espluque.Contracts.MessageInterfaces;
using Espluque.Contracts.ModuleInterfaces.Contributions;
using Espluque.Contracts.Ports;
using System.IO;

namespace CompositeMdModule
{
    public class ModuleService : IDetector, IGrabber, IWpfViewer, IManagedDependencies
    {
        private readonly IMessageCenter _messageCenter;
        private readonly ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        private readonly string _referentiel = "Espluque";

        public ModuleService()
        {

        }

        public ModuleService(IMessageCenter messageCenter,
            ILogger logger,
            ISettingsService settingsService,
            IEntityFactory entityFactory)
        {
            _messageCenter = messageCenter;
            _logger = logger;
            _settingsService = settingsService;
            _entityFactory = entityFactory;
        }

        public async Task<IFileFormat> Detect(AnalysisContext analysisContext)
        {
            IFileFormat fileFormat = _entityFactory.CreateFileFormat(
                    _referentiel,
                    string.Empty,
                    null,
                    null);

            string extension = Path.GetExtension(analysisContext.FilePath);
            bool isMarkdown = string.Equals(extension, ".md", StringComparison.OrdinalIgnoreCase);

            if (isMarkdown)
            {
                fileFormat.Label = "Markdown";
            }

            return fileFormat;
        }

        public async Task<List<KeyValuePair<string, string>>> Grab(AnalysisContext analysisContext)
        {
            List<KeyValuePair<string, string>> infos = [];

            var linesCount = File.ReadLines(analysisContext.FilePath).LongCount();

            infos.Add(new("Lines", linesCount.ToString()));

            return infos;
        }

        public Task<object?> GetViewer(AnalysisContext analysisContext)
        {
            object viewer = new MdViewerUC(analysisContext.FilePath);
            return Task.FromResult<object?>(viewer);
        }

        public List<string> GetDependencyPaths()
        {
            /// Espluque loads depencies when starting to prevent XAML parser bug
            /// Xaml parser doesn't work with AssemblyLoadContext #1700
            /// https://github.com/dotnet/wpf/issues/1700

            string moduleRootPath = Path.GetDirectoryName(typeof(ModuleService).Assembly.Location)!;

            List<string> paths =
            [
                Path.Combine(moduleRootPath, "Markdig.dll"),
                Path.Combine(moduleRootPath, "Markdig.Wpf.dll")
            ];

            return paths;
        }
    }

}
