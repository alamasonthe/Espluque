using Espluque.Contracts.Contributions.Types;
using Espluque.Contracts.CrossCutting;
using Espluque.Contracts.Workflow;

namespace Shortcut
{
    public class ShortcutInfosGrabber: IGrabber
    {

        private readonly IMessageCenter _messageCenter;
        private readonly ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        public ShortcutInfosGrabber(IMessageCenter messageCenter,
            ILogger logger,
            ISettingsService settingsService,
            IEntityFactory entityFactory)
        {
            _messageCenter = messageCenter;
            _logger = logger;
            _settingsService = settingsService;
            _entityFactory = entityFactory;
        }

        public async Task<List<KeyValuePair<string, string>>> Grab(IAnalysisContext analysisContext)
        {
            var fileName = Path.GetFileName(analysisContext.FilePath).PadRight(35);
            var infosResult = Grabber.GetShortcutInfos(analysisContext.FilePath);
            if (!infosResult.IsSuccess)
            {
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"{fileName}\t{infosResult.Error.Code} {infosResult.Error.Message}");
            }
            return new Formatter().Format(infosResult.Value);
        }

    }
}
