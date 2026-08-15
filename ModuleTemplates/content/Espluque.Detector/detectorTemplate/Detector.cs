using Espluque.Contracts.Interfaces;
using Espluque.Contracts.Contributions.Types;
using Espluque.Contracts.MessageInterfaces;
using Espluque.Contracts.Ports;
using Espluque.Contracts.Workflow;
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

        public Task<IFileFormat> Detect(IAnalysisContext analysisContext)
        {
            var formattedFileName = Path.GetFileName(analysisContext.FilePath).PadRight(35);

            IFileFormat fileFormat = _entityFactory.CreateFileFormat(
                _referentiel,
                string.Empty,
                null,
                null);

            try
            {

                if (File.Exists(analysisContext.FilePath))
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
