using Espluque.Contracts.Interfaces;
using Espluque.Contracts.MessageInterfaces;
using Espluque.Contracts.Ports;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using Espluque.Contracts.ModuleInterfaces.Contributions;

namespace Pronom
{
    internal class Detector: IDetector
    {
        private readonly IMessageCenter _messageCenter;
        private readonly Espluque.Contracts.Ports.ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        private readonly string _referentiel = "Pronom";

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

            FormatIdentifier formatIdentifier = new(_logger, _entityFactory);
            var dbFilePath = GetDbFilePath(_settingsService);
            var fileFormatResult = await formatIdentifier.MatchSignaturesAsync(filePath, dbFilePath);
            if (fileFormatResult.IsSuccess)
            {
                return fileFormatResult.Value;
            }
            else
            {
                _logger.Log(LogLevel.Error, $"{formatIdentifier}\tPronom detector: {fileFormatResult.Error.Code} {fileFormatResult.Error.Message}");
                return _entityFactory.CreateFileFormat(_referentiel, string.Empty, null, null);
            }
        }

        private static string GetDbFilePath(ISettingsService settingsService)
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appName = Assembly.GetEntryAssembly()?.GetName().Name ?? Process.GetCurrentProcess().ProcessName;
            string appDirectoryPath = Path.Combine(appDataPath, appName);

            string? settingsDbFileName = settingsService
                .GetSetting("PronomDb")
                .GetAwaiter()
                .GetResult();

            if (string.IsNullOrWhiteSpace(settingsDbFileName))
            {
                settingsDbFileName = "pronom.db";
            }

            string dbFilePath;

            if (Path.IsPathRooted(settingsDbFileName))
            {
                dbFilePath = settingsDbFileName;
            }
            else
            {
                dbFilePath = Path.Combine(appDirectoryPath, settingsDbFileName);
            }

            string? directoryPath = Path.GetDirectoryName(dbFilePath);

            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            return dbFilePath;
        }
    }
}
