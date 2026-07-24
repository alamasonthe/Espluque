using Espluque.Contracts.Interfaces;
using Espluque.Contracts.MessageInterfaces;
using Espluque.Contracts.ModuleInterfaces.Contributions;
using Espluque.Contracts.Ports;
using Microsoft.Extensions.Logging;

namespace detectorTemplate
{
    public class Detector : IDetector
    {
        private readonly IMessageCenter _messageCenter;
        private readonly Espluque.Contracts.Ports.ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        private readonly string _referentiel = "TemplateReferentiel";

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

        public Task<IFileFormat> Detect(string filePath)
        {
            var formattedFileName = Path.GetFileName(filePath).PadRight(35);

            IFileFormat fileFormat = _entityFactory.CreateFileFormat(
                _referentiel,
                string.Empty,
                null,
                null);

            try
            {

                if (File.Exists(filePath))
                {
                    fileFormat.Label = "ThisIsAFile";
                    fileFormat.Version = string.Empty;
                    fileFormat.MIMEType = string.Empty;
                }

            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"{formattedFileName}\tdetectorTemplate error: {ex.Message}");
            }

            return Task.FromResult(fileFormat);
        }
    }
}
