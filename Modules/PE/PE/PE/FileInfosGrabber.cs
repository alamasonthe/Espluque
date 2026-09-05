using Espluque.Contracts.Contributions.Types;
using Espluque.Contracts.CrossCutting;
using Espluque.Contracts.Workflow;
using PE.Services;

namespace PE
{
    public class FileInfosGrabber : IGrabber
    {
        private readonly IMessageCenter _messageCenter;
        private readonly Espluque.Contracts.CrossCutting.ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        public FileInfosGrabber(IMessageCenter messageCenter,
            Espluque.Contracts.CrossCutting.ILogger logger,
            ISettingsService settingsService,
            IEntityFactory entityFactory)
        {
            _messageCenter = messageCenter;
            _logger = logger;
            _settingsService = settingsService;
            _entityFactory = entityFactory;
        }

        public Task<List<KeyValuePair<string, string>>> Grab(IAnalysisContext analysisContext)
        {
            var infos = new FileSystemService().GetFileInfos(analysisContext.FilePath ?? string.Empty);

            return Task.FromResult(infos);
        }
    }
}
