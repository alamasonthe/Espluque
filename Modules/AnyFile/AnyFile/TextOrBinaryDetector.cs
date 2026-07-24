using Espluque.Contracts.Interfaces;
using Espluque.Contracts.MessageInterfaces;
using Espluque.Contracts.Ports;
using Microsoft.Extensions.Logging;
using Util;
using Util.Enums;
using Espluque.Contracts.ModuleInterfaces.Contributions;

namespace AnyFile
{
    public class TextOrBinaryDetector: IDetector
    {
        private readonly IMessageCenter _messageCenter;
        private readonly Espluque.Contracts.Ports.ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        private readonly string _referentiel = "Espluque";

        public TextOrBinaryDetector(IMessageCenter messageCenter,
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

            IFileFormat fileFormat = _entityFactory.CreateFileFormat(
                    _referentiel,
                    string.Empty,
                    null,
                    null);

            try
            {

                Result<byte[]> byteSampleResult = Bin.ReadBytesFromFile(filePath, 0, 4096);

                if (!byteSampleResult.IsSuccess)
                {
                    _logger.Log(LogLevel.Error, $"{formattedFileName}\tTextOrBinary Detect error: Cannot read file");
                    return fileFormat;
                }

                byte[] byteSample = byteSampleResult.Value ?? [];

                Result<TextBinaryEnum> textOrBinaryResult = Bin.FromBytes(byteSample).DetectTextOrBinary();

                if (!textOrBinaryResult.IsSuccess)
                {
                    _logger.Log(LogLevel.Error, $"{formattedFileName}\tTextOrBinary Detect error: {textOrBinaryResult.Error.Code} {textOrBinaryResult.Error.Message}");
                    return fileFormat;
                }

                bool isBinary = textOrBinaryResult.Value == TextBinaryEnum.Binary;

                if (isBinary)
                {
                    fileFormat.Label = "Binary";
                }
                else
                {
                    fileFormat.Label = "Text";
                }

                return fileFormat;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"{formattedFileName}\tTextOrBinary Detect error: {ex.Message}");
                return fileFormat;
            }
        }
    }
}
