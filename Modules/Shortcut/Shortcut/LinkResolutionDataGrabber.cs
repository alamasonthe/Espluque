using Espluque.Contracts.Contributions.Types;
using Espluque.Contracts.CrossCutting;
using Espluque.Contracts.Workflow;

namespace Shortcut
{
    internal class LinkResolutionDataGrabber : IGrabber
    {
        private readonly IMessageCenter _messageCenter;
        private readonly ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        public LinkResolutionDataGrabber(IMessageCenter messageCenter,
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
            var linkResolutionDataResult = await Grabber.GetLinkResolutionData(analysisContext.FilePath);

            if (!linkResolutionDataResult.IsSuccess)
            {
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"{fileName}\t{linkResolutionDataResult.Error.Code} {linkResolutionDataResult.Error.Message}");
                return null;
            }

            return new Formatter().Format(linkResolutionDataResult.Value);
        }
    }
}
