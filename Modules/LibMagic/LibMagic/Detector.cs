using Espluque.Contracts.Contributions;
using Espluque.Contracts.Contributions.Types;
using Espluque.Contracts.CrossCutting;
using Espluque.Contracts.Workflow;
using HeyRed.Mime;
using Microsoft.Extensions.Logging;

namespace LibMagic
{
    public class Detector : IDetector
    {
        private readonly IMessageCenter _messageCenter;
        private readonly Espluque.Contracts.CrossCutting.ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        private readonly string _referentiel = "Libmagic";

        public Detector(IMessageCenter messageCenter,
            Espluque.Contracts.CrossCutting.ILogger logger,
            ISettingsService settingsService,
            IEntityFactory entityFactory)
        {
            _messageCenter = messageCenter;
            _logger = logger;
            _settingsService = settingsService;
            _entityFactory = entityFactory;
        }

        public async Task<IFileFormat> Detect(IAnalysisContext analysisContext)
        {
            var formattedFileName = Path.GetFileName(analysisContext.FilePath).PadRight(35);
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

                string label = magic.Read(analysisContext.FilePath);
                string mimeType = MimeGuesser.GuessMimeType(analysisContext.FilePath);

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
