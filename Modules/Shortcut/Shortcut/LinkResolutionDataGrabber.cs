using Espluque.Contracts.Interfaces;
using Espluque.Contracts.MessageInterfaces;
using Espluque.Contracts.ModuleInterfaces.Contributions;
using Espluque.Contracts.Ports;

namespace Shortcut
{
    internal class LinkResolutionDataGrabber : IGrabber
    {
        private readonly IMessageCenter _messageCenter;
        private readonly Espluque.Contracts.Ports.ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        public LinkResolutionDataGrabber(IMessageCenter messageCenter,
            Espluque.Contracts.Ports.ILogger logger,
            ISettingsService settingsService,
            IEntityFactory entityFactory)
        {
            _messageCenter = messageCenter;
            _logger = logger;
            _settingsService = settingsService;
            _entityFactory = entityFactory;
        }

        public async Task<List<KeyValuePair<string, string>>> Grab(string filePath)
        {
            var fileName = Path.GetFileName(filePath).PadRight(35);
            var linkResolutionDataResult = await Grabber.GetLinkResolutionData(filePath);

            if (!linkResolutionDataResult.IsSuccess)
            {
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"{fileName}\t{linkResolutionDataResult.Error.Code} {linkResolutionDataResult.Error.Message}");
                return null;
            }

            return new Formatter().Format(linkResolutionDataResult.Value);
        }
    }
}
