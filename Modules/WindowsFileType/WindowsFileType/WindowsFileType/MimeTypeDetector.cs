using Espluque.Contracts.Contributions;
using Espluque.Contracts.Contributions.Types;
using Espluque.Contracts.CrossCutting;
using Espluque.Contracts.Workflow;
using Microsoft.Extensions.Logging;

namespace WindowsFileType
{
    public class MimeTypeDetector: IDetector
    {
        private readonly IMessageCenter _messageCenter;
        private readonly Espluque.Contracts.CrossCutting.ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        private readonly string _referentiel = "MIMEType";

        public MimeTypeDetector(
            IMessageCenter messageCenter,
            Espluque.Contracts.CrossCutting.ILogger logger,
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
                string? mime = MimeDetectionService.DetectMimeType(analysisContext.FilePath);

                _logger.Log( LogLevel.Debug, $"{formattedFileName}\tWindows MIME detection: {mime ?? "<null>"}");

                if (!(string.IsNullOrWhiteSpace(mime)))
                {
                    fileFormat.Label = string.Empty;
                    fileFormat.Version = string.Empty;
                    fileFormat.MIMEType = mime;
                }
            }
            catch (Exception ex)
            {
                _logger.Log( LogLevel.Error, $"{formattedFileName}\tWindowsMimeDetection error: {ex.Message}");
            }

            return Task.FromResult(fileFormat);
        }
    }
}
