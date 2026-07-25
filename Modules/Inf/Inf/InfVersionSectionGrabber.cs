using Espluque.Contracts.Entities;
using Espluque.Contracts.Interfaces;
using Espluque.Contracts.MessageInterfaces;
using Espluque.Contracts.ModuleInterfaces.Contributions;
using Espluque.Contracts.Ports;
using Microsoft.Extensions.Logging;
using Util;

namespace Inf
{
    internal class InfVersionSectionGrabber: IGrabber
    {
        private readonly IMessageCenter _messageCenter;
        private readonly Espluque.Contracts.Ports.ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        public InfVersionSectionGrabber(IMessageCenter messageCenter,
            Espluque.Contracts.Ports.ILogger logger,
            ISettingsService settingsService,
            IEntityFactory entityFactory)
        {
            _messageCenter = messageCenter;
            _logger = logger;
            _settingsService = settingsService;
            _entityFactory = entityFactory;
        }

        public async Task<List<KeyValuePair<string, string>>> Grab(AnalysisContext analysisContext)
        {
            string fileName = Path.GetFileName(analysisContext.FilePath).PadRight(35);

            Result<List<KeyValuePair<string, string>>> versionInfosResult = await Grabber.GetVersionSectionInfos(analysisContext.FilePath);

            if (!versionInfosResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"{fileName}\t{versionInfosResult.Error.Code} {versionInfosResult.Error.Message}");
                return null;
            }

            return new Formatter().Format(versionInfosResult.Value);
        }
    }
}
