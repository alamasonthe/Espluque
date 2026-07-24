using Espluque.Contracts.Interfaces;
using Espluque.Contracts.MessageInterfaces;
using Espluque.Contracts.ModuleInterfaces.Contributions;
using Espluque.Contracts.Ports;
using HeyRed.Mime;
using Microsoft.Extensions.Logging;

namespace LibMagic
{
    public class Detector : IDetector
    {
        private readonly IMessageCenter _messageCenter;
        private readonly Espluque.Contracts.Ports.ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        private readonly string _referentiel = "Libmagic";

        public Detector(IMessageCenter messageCenter,
            Espluque.Contracts.Ports.ILogger logger,
            ISettingsService settingsService,
            IEntityFactory entityFactory)
        {
            _messageCenter = messageCenter;
            _logger = logger;
            _settingsService = settingsService;
            _entityFactory = entityFactory;
        }

        public async Task<IFileFormat> Detect(string filePath)
        {
            var formattedFileName = Path.GetFileName(filePath).PadRight(35);
            _logger.Log(LogLevel.Debug, $"{formattedFileName}\tLibmagic detection start");

            IFileFormat fileFormat = _entityFactory.CreateFileFormat(
                    _referentiel,
                    string.Empty,
                    null,
                    null);

            try
            {
                MimeConfiguration.Configure();

                using Magic magic = new(MagicOpenFlags.MAGIC_NONE);

                string label = magic.Read(filePath);
                string mimeType = MimeGuesser.GuessMimeType(filePath);

                fileFormat = _entityFactory.CreateFileFormat(
                    _referentiel,
                    label,
                    null,
                    mimeType);

                _logger.Log(LogLevel.Information, $"{formattedFileName}\tLibmagic detection: {mimeType}\t{label}");
                return fileFormat;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"{formattedFileName}\tLibmagic Detect error: {ex.Message}");
                return fileFormat;
            }
        }
    }
}
