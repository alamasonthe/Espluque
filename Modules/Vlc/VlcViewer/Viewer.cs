using Espluque.Contracts.Interfaces;
using Espluque.Contracts.MessageInterfaces;
using Espluque.Contracts.ModuleInterfaces.Contributions;
using Espluque.Contracts.Ports;
using LibVLCSharp.Shared;
using System.IO;

namespace VlcViewer
{
    public class Viewer : IWpfViewer
    {
        private readonly IMessageCenter _messageCenter;
        private readonly ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        public Viewer(IMessageCenter messageCenter, ILogger logger, ISettingsService settingsService, IEntityFactory entityFactory)
        {
            _messageCenter = messageCenter;
            _logger = logger;
            _settingsService = settingsService;
            _entityFactory = entityFactory;
        }

        public async Task<object?> GetViewer(IAnalysisContext analysisContext)
        {
            string formattedFileName = Path.GetFileName(analysisContext.FilePath).PadRight(35);

            bool canRead = await CanRead(analysisContext.FilePath);

            if (!canRead)
            {
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Debug, $"{formattedFileName}\tVLC cannot read this file");
                return null;
            }

            return new VlcUC(analysisContext.FilePath);
        }

        private static async Task<bool> CanRead(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return false;
            }

            FileInfo fileInfo = new FileInfo(filePath);

            if (fileInfo.Length == 0)
            {
                return false;
            }

            await Task.Run(InitializeVlcDll);

            using LibVLC libVLC = new LibVLC("--quiet");
            using Media media = new Media(libVLC, filePath, FromType.FromPath);

            MediaParsedStatus parsedStatus = await media.Parse(MediaParseOptions.ParseLocal, timeout: 3000);

            if (parsedStatus != MediaParsedStatus.Done)
            {
                return false;
            }

            bool hasVideoTrack = media.Tracks.Any(track => track.TrackType == TrackType.Video);

            return hasVideoTrack;
        }

        private static readonly object InitializeVlcDllLock = new();
        private static bool _vlcInitialized;

        private static void InitializeVlcDll()
        {
            if (_vlcInitialized)
            {
                return;
            }

            lock (InitializeVlcDllLock)
            {
                if (_vlcInitialized)
                {
                    return;
                }

                string moduleDirectory = Path.GetDirectoryName(typeof(Viewer).Assembly.Location)!;

                Core.Initialize(moduleDirectory);

                _vlcInitialized = true;
            }
        }
    }

}
