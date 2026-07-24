using Espluque.Contracts.Interfaces;
using Espluque.Contracts.MessageInterfaces;
using Espluque.Contracts.ModuleInterfaces.Contributions;
using Espluque.Contracts.Ports;
using FFmpeg.AutoGen;
using Util;

namespace FFmpeg
{
    internal class MediaInfosGrabber : IGrabber
    {
        private readonly IMessageCenter _messageCenter;
        private readonly Espluque.Contracts.Ports.ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        public MediaInfosGrabber(IMessageCenter messageCenter,
            Espluque.Contracts.Ports.ILogger logger,
            ISettingsService settingsService,
            IEntityFactory entityFactory)
        {
            _messageCenter = messageCenter;
            _logger = logger;
            _settingsService = settingsService;
            _entityFactory = entityFactory;
        }

        public async Task<List<KeyValuePair<string, string>>> Grab(string filePath)
        {
            string fileName = Path.GetFileName(filePath).PadRight(35);
            List<KeyValuePair<string, string>> infos = [];

            IntPtr mediaContextPointer = 0;

            Result<IntPtr> openMediaContextResult = await Grabber.OpenMediaContext(filePath);

            if (!openMediaContextResult.IsSuccess)
            {
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"{fileName}\t{openMediaContextResult.Error.Code} {openMediaContextResult.Error.Message}");
                return null;
            }

            mediaContextPointer = openMediaContextResult.Value;

            try
            {
                Result<bool> loadStreamInfosResult = await Grabber.LoadStreamInfos(mediaContextPointer);

                if (!loadStreamInfosResult.IsSuccess)
                {
                    _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"{fileName}\t{loadStreamInfosResult.Error.Code} {loadStreamInfosResult.Error.Message}");
                    return null;
                }

                Result<List<KeyValuePair<string, string>>?> containerInfosResult = await Grabber.GetContainerInfos(mediaContextPointer);

                if (!containerInfosResult.IsSuccess)
                {
                    _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"{fileName}\t{containerInfosResult.Error.Code} {containerInfosResult.Error.Message}");
                    return null;
                }

                if (containerInfosResult.Value is not null)
                {
                    infos.AddRange(containerInfosResult.Value);
                }

                Result<uint> streamCountResult = await Grabber.GetStreamCount(mediaContextPointer);

                if (!streamCountResult.IsSuccess)
                {
                    _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"{fileName}\t{streamCountResult.Error.Code} {streamCountResult.Error.Message}");
                    return null;
                }

                for (uint streamIndex = 0; streamIndex < streamCountResult.Value; streamIndex++)
                {
                    Result<AVMediaType> streamTypeResult = await Grabber.GetStreamType(mediaContextPointer, streamIndex);

                    if (!streamTypeResult.IsSuccess)
                    {
                        _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"{fileName}\t{streamTypeResult.Error.Code} {streamTypeResult.Error.Message}");
                        return null;
                    }

                    Result<List<KeyValuePair<string, string>>?> streamCodecInfosResult = await Grabber.GetStreamCodecInfos(mediaContextPointer, streamIndex);

                    if (!streamCodecInfosResult.IsSuccess)
                    {
                        _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"{fileName}\t{streamCodecInfosResult.Error.Code} {streamCodecInfosResult.Error.Message}");
                        return null;
                    }

                    if (streamCodecInfosResult.Value is not null)
                    {
                        infos.AddRange(streamCodecInfosResult.Value);
                    }

                    if (streamTypeResult.Value == AVMediaType.AVMEDIA_TYPE_VIDEO)
                    {
                        Result<List<KeyValuePair<string, string>>?> videoStreamInfosResult = await Grabber.GetVideoStreamInfos(mediaContextPointer, streamIndex);

                        if (!videoStreamInfosResult.IsSuccess)
                        {
                            _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"{fileName}\t{videoStreamInfosResult.Error.Code} {videoStreamInfosResult.Error.Message}");
                            return null;
                        }

                        if (videoStreamInfosResult.Value is not null)
                        {
                            infos.AddRange(videoStreamInfosResult.Value);
                        }
                    }

                    if (streamTypeResult.Value == AVMediaType.AVMEDIA_TYPE_AUDIO)
                    {
                        Result<List<KeyValuePair<string, string>>?> audioStreamInfosResult = await Grabber.GetAudioStreamInfos(mediaContextPointer, streamIndex);

                        if (!audioStreamInfosResult.IsSuccess)
                        {
                            _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"{fileName}\t{audioStreamInfosResult.Error.Code} {audioStreamInfosResult.Error.Message}");
                            return null;
                        }

                        if (audioStreamInfosResult.Value is not null)
                        {
                            infos.AddRange(audioStreamInfosResult.Value);
                        }
                    }
                }

                return new Formatter().Format(infos);
            }
            finally
            {
                Result<bool> closeMediaContextResult = await Grabber.CloseMediaContext(mediaContextPointer);

                if (!closeMediaContextResult.IsSuccess)
                {
                    _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"{fileName}\t{closeMediaContextResult.Error.Code} {closeMediaContextResult.Error.Message}");
                }
            }
        }

    }
}
