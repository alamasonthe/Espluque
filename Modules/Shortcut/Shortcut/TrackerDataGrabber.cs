using Espluque.Contracts.Contributions.Types;
using Espluque.Contracts.CrossCutting;
using Espluque.Contracts.Workflow;

namespace Shortcut
{
    internal class TrackerDataGrabber: IGrabber
    {
        private readonly IMessageCenter _messageCenter;
        private readonly ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        public TrackerDataGrabber(IMessageCenter messageCenter,
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
            var trackerDataResult = await Grabber.GetTrackerData(analysisContext.FilePath);

            if (!trackerDataResult.IsSuccess)
            {
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"{fileName}\t{trackerDataResult.Error.Code} {trackerDataResult.Error.Message}");
                return null;
            }

            return new Formatter().Format(trackerDataResult.Value);
        }
    }
}
