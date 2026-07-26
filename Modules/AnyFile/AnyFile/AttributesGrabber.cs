using Espluque.Contracts.Entities;
using Espluque.Contracts.Interfaces;
using Espluque.Contracts.MessageInterfaces;
using Espluque.Contracts.ModuleInterfaces.Contributions;
using Espluque.Contracts.Ports;

namespace AnyFile
{
    public class AttributesGrabber: IGrabber
    {
        private readonly IMessageCenter _messageCenter;
        private readonly Espluque.Contracts.Ports.ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        public AttributesGrabber(IMessageCenter messageCenter,
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
            try
            {
                FileInfo fileInfo = new FileInfo(analysisContext.FilePath);
                FileAttributes attributes = fileInfo.Attributes;

                List<KeyValuePair<string, string>> infos = new List<KeyValuePair<string, string>>();

                FileAttributes[] attributesToCheck =
                [
                    FileAttributes.Archive,
                    FileAttributes.Compressed,
                    FileAttributes.Directory,
                    FileAttributes.Encrypted,
                    FileAttributes.Hidden,
                    FileAttributes.IntegrityStream,
                    FileAttributes.Normal,
                    FileAttributes.NoScrubData,
                    FileAttributes.NotContentIndexed,
                    FileAttributes.Offline,
                    FileAttributes.ReadOnly,
                    FileAttributes.ReparsePoint,
                    FileAttributes.SparseFile,
                    FileAttributes.System,
                    FileAttributes.Temporary
                ];

                foreach (FileAttributes attribute in attributesToCheck)
                {
                    bool hasAttribute = attribute == FileAttributes.Normal
                        ? attributes == FileAttributes.Normal
                        : (attributes & attribute) == attribute;

                    infos.Add(new KeyValuePair<string, string>(
                        attribute.ToString(),
                        hasAttribute ? "Yes" : "No"));
                }

                return new Formatter().Format(infos);
            }
            catch (Exception ex)
            {
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"AnyFile module Attributes Reader: {ex.Message}");
                return new List<KeyValuePair<string, string>>();
            }
        }
    }
}
