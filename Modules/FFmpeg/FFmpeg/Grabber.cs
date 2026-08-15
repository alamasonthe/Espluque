using Espluque.Contracts.CrossCutting;
using FFmpeg.AutoGen;
using System.Runtime.InteropServices;
using Util;

namespace FFmpeg
{
    internal class Grabber
    {
        public static async Task<Result<List<KeyValuePair<string, string>>?>> GetVersions()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // string ffmpegDllFolder = System.IO.Path.Combine(AppContext.BaseDirectory, "ffmpeg");
                    string ffmpegDllFolder = RuntimePaths.NativeFFmpegDirectoryPath;

                    if (!System.IO.Directory.Exists(ffmpegDllFolder))
                    {
                        return Result<List<KeyValuePair<string, string>>?>.Failure("FFMPEG_DLL_FOLDER_NOT_FOUND", "FFmpeg DLL folder does not exist.");
                    }

                    ffmpeg.RootPath = ffmpegDllFolder;

                    List<KeyValuePair<string, string>> infos =
                    [
                        new KeyValuePair<string, string>("FFmpeg version", ffmpeg.av_version_info()),
                        new KeyValuePair<string, string>("libavutil version", FormatVersion(ffmpeg.avutil_version())),
                        new KeyValuePair<string, string>("libavcodec version", FormatVersion(ffmpeg.avcodec_version())),
                        new KeyValuePair<string, string>("libavformat version", FormatVersion(ffmpeg.avformat_version()))
                    ];

                    return Result<List<KeyValuePair<string, string>>?>.Success(infos);
                }
                catch (DllNotFoundException ex)
                {
                    return Result<List<KeyValuePair<string, string>>?>.Failure("FFMPEG_DLL_NOT_FOUND", ex.Message);
                }
                catch (BadImageFormatException ex)
                {
                    return Result<List<KeyValuePair<string, string>>?>.Failure("FFMPEG_DLL_BAD_FORMAT", ex.Message);
                }
                catch (EntryPointNotFoundException ex)
                {
                    return Result<List<KeyValuePair<string, string>>?>.Failure("FFMPEG_ENTRY_POINT_NOT_FOUND", ex.Message);
                }
                catch (Exception ex)
                {
                    return Result<List<KeyValuePair<string, string>>?>.Failure("FFMPEG_VERSION_READ_FAILED", ex.Message);
                }
            });
        }

        public static async Task<Result<List<KeyValuePair<string, string>>?>> GetMediaInfos(string filePath)
        {
            unsafe
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(filePath))
                    {
                        return Result<List<KeyValuePair<string, string>>?>.Failure("FFMPEG_FILEPATH_MISSING", "File path is missing.");
                    }

                    if (!System.IO.File.Exists(filePath))
                    {
                        return Result<List<KeyValuePair<string, string>>?>.Failure("FFMPEG_FILE_NOT_FOUND", "File does not exist.");
                    }

                    string ffmpegDllFolder = System.IO.Path.Combine(AppContext.BaseDirectory, "ffmpeg");

                    if (!System.IO.Directory.Exists(ffmpegDllFolder))
                    {
                        return Result<List<KeyValuePair<string, string>>?>.Failure("FFMPEG_DLL_FOLDER_NOT_FOUND", "FFmpeg DLL folder does not exist.");
                    }

                    ffmpeg.RootPath = ffmpegDllFolder;

                    AVFormatContext* formatContext = null;

                    int openResult = ffmpeg.avformat_open_input(&formatContext, filePath, null, null);

                    if (openResult < 0)
                    {
                        return Result<List<KeyValuePair<string, string>>?>.Failure("FFMPEG_OPEN_INPUT_FAILED", GetFFmpegError(openResult));
                    }

                    try
                    {
                        int streamInfoResult = ffmpeg.avformat_find_stream_info(formatContext, null);

                        if (streamInfoResult < 0)
                        {
                            return Result<List<KeyValuePair<string, string>>?>.Failure("FFMPEG_STREAM_INFO_FAILED", GetFFmpegError(streamInfoResult));
                        }

                        List<KeyValuePair<string, string>> infos =
                        [
                            new KeyValuePair<string, string>("Format name", PtrToString(formatContext->iformat->name)),
                            new KeyValuePair<string, string>("Format long name", PtrToString(formatContext->iformat->long_name)),
                            new KeyValuePair<string, string>("Duration", FormatDuration(formatContext->duration)),
                            new KeyValuePair<string, string>("Bit rate", formatContext->bit_rate.ToString()),
                            new KeyValuePair<string, string>("Stream count", formatContext->nb_streams.ToString())
                        ];

                        for (uint streamIndex = 0; streamIndex < formatContext->nb_streams; streamIndex++)
                        {
                            AVStream* stream = formatContext->streams[streamIndex];
                            AVCodecParameters* codecParameters = stream->codecpar;

                            string prefix = $"Stream {streamIndex}";

                            infos.Add(new KeyValuePair<string, string>($"{prefix} Type", codecParameters->codec_type.ToString()));
                            infos.Add(new KeyValuePair<string, string>($"{prefix} Codec", ffmpeg.avcodec_get_name(codecParameters->codec_id)));

                            AVCodecDescriptor* codecDescriptor = ffmpeg.avcodec_descriptor_get(codecParameters->codec_id);

                            if (codecDescriptor is not null)
                            {
                                infos.Add(new KeyValuePair<string, string>($"{prefix} Codec long name", PtrToString(codecDescriptor->long_name)));
                            }

                            if (codecParameters->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                            {
                                infos.Add(new KeyValuePair<string, string>($"{prefix} Width", codecParameters->width.ToString()));
                                infos.Add(new KeyValuePair<string, string>($"{prefix} Height", codecParameters->height.ToString()));
                                infos.Add(new KeyValuePair<string, string>($"{prefix} Pixel format", ((AVPixelFormat)codecParameters->format).ToString()));
                                infos.Add(new KeyValuePair<string, string>($"{prefix} Average frame rate", FormatRational(stream->avg_frame_rate)));
                                infos.Add(new KeyValuePair<string, string>($"{prefix} Real frame rate", FormatRational(stream->r_frame_rate)));
                            }

                            if (codecParameters->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
                            {
                                infos.Add(new KeyValuePair<string, string>($"{prefix} Sample rate", codecParameters->sample_rate.ToString()));
                                infos.Add(new KeyValuePair<string, string>($"{prefix} Channels", codecParameters->ch_layout.nb_channels.ToString()));
                                infos.Add(new KeyValuePair<string, string>($"{prefix} Sample format", ((AVSampleFormat)codecParameters->format).ToString()));
                            }
                        }

                        return Result<List<KeyValuePair<string, string>>?>.Success(infos);
                    }
                    finally
                    {
                        ffmpeg.avformat_close_input(&formatContext);
                    }
                }
                catch (DllNotFoundException ex)
                {
                    return Result<List<KeyValuePair<string, string>>?>.Failure("FFMPEG_DLL_NOT_FOUND", ex.Message);
                }
                catch (BadImageFormatException ex)
                {
                    return Result<List<KeyValuePair<string, string>>?>.Failure("FFMPEG_DLL_BAD_FORMAT", ex.Message);
                }
                catch (EntryPointNotFoundException ex)
                {
                    return Result<List<KeyValuePair<string, string>>?>.Failure("FFMPEG_ENTRY_POINT_NOT_FOUND", ex.Message);
                }
                catch (Exception ex)
                {
                    return Result<List<KeyValuePair<string, string>>?>.Failure("FFMPEG_MEDIA_INFO_READ_FAILED", ex.Message);
                }
            }
        }

        #region Special

        public static unsafe async Task<Result<IntPtr>> OpenMediaContext(string filePath)
        {
            try
            {
                string ffmpegDllFolder = Path.GetDirectoryName(typeof(Grabber).Assembly.Location)!;

                if (!System.IO.Directory.Exists(ffmpegDllFolder))
                {
                    return Result<IntPtr>.Failure("FFMPEG_DLL_FOLDER_NOT_FOUND", "FFmpeg DLL folder does not exist.");
                }

                ffmpeg.RootPath = ffmpegDllFolder;

                AVFormatContext* formatContext = null;

                int openResult = ffmpeg.avformat_open_input(&formatContext, filePath, null, null);

                if (openResult < 0)
                {
                    return Result<IntPtr>.Failure("FFMPEG_OPEN_INPUT_FAILED", GetFFmpegError(openResult));
                }

                return Result<IntPtr>.Success((IntPtr)formatContext);
            }
            catch (Exception ex)
            {
                return Result<IntPtr>.Failure("FFMPEG_OPEN_MEDIA_CONTEXT_FAILED", ex.Message);
            }
        }

        public static unsafe async Task<Result<bool>> LoadStreamInfos(nint mediaContextPointer)
        {
            try
            {
                AVFormatContext* formatContext = (AVFormatContext*)mediaContextPointer;

                int streamInfoResult = ffmpeg.avformat_find_stream_info(formatContext, null);

                if (streamInfoResult < 0)
                {
                    return Result<bool>.Failure("FFMPEG_STREAM_INFO_FAILED", GetFFmpegError(streamInfoResult));
                }

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure("FFMPEG_STREAM_INFO_READ_FAILED", ex.Message);
            }
        }

        public static unsafe async Task<Result<List<KeyValuePair<string, string>>?>> GetContainerInfos(nint mediaContextPointer)
        {
            try
            {
                AVFormatContext* formatContext = (AVFormatContext*)mediaContextPointer;

                List<KeyValuePair<string, string>> infos =
                [
                    new KeyValuePair<string, string>("Format name", PtrToString(formatContext->iformat->name)),
                    new KeyValuePair<string, string>("Format long name", PtrToString(formatContext->iformat->long_name)),
                    new KeyValuePair<string, string>("Duration", FormatDuration(formatContext->duration)),
                    new KeyValuePair<string, string>("Bit rate", formatContext->bit_rate.ToString()),
                    new KeyValuePair<string, string>("Stream count", formatContext->nb_streams.ToString())
                ];

                return Result<List<KeyValuePair<string, string>>?>.Success(infos);
            }
            catch (Exception ex)
            {
                return Result<List<KeyValuePair<string, string>>?>.Failure("FFMPEG_CONTAINER_INFO_READ_FAILED", ex.Message);
            }
        }

        public static unsafe async Task<Result<uint>> GetStreamCount(nint mediaContextPointer)
        {
            try
            {
                AVFormatContext* formatContext = (AVFormatContext*)mediaContextPointer;

                return Result<uint>.Success(formatContext->nb_streams);
            }
            catch (Exception ex)
            {
                return Result<uint>.Failure("FFMPEG_STREAM_COUNT_READ_FAILED", ex.Message);
            }
        }

        public static unsafe async Task<Result<AVMediaType>> GetStreamType(nint mediaContextPointer, uint streamIndex)
        {
            try
            {
                AVFormatContext* formatContext = (AVFormatContext*)mediaContextPointer;
                AVStream* stream = formatContext->streams[streamIndex];
                AVCodecParameters* codecParameters = stream->codecpar;

                return Result<AVMediaType>.Success(codecParameters->codec_type);
            }
            catch (Exception ex)
            {
                return Result<AVMediaType>.Failure("FFMPEG_STREAM_TYPE_READ_FAILED", ex.Message);
            }
        }

        public static unsafe async Task<Result<List<KeyValuePair<string, string>>?>> GetStreamCodecInfos(nint mediaContextPointer, uint streamIndex)
        {
            try
            {
                AVFormatContext* formatContext = (AVFormatContext*)mediaContextPointer;
                AVStream* stream = formatContext->streams[streamIndex];
                AVCodecParameters* codecParameters = stream->codecpar;

                string prefix = $"Stream {streamIndex}";

                List<KeyValuePair<string, string>> infos =
                [
                    new KeyValuePair<string, string>($"{prefix} Type", codecParameters->codec_type.ToString()),
            new KeyValuePair<string, string>($"{prefix} Codec", ffmpeg.avcodec_get_name(codecParameters->codec_id))
                ];

                AVCodecDescriptor* codecDescriptor = ffmpeg.avcodec_descriptor_get(codecParameters->codec_id);

                if (codecDescriptor is not null)
                {
                    infos.Add(new KeyValuePair<string, string>($"{prefix} Codec long name", PtrToString(codecDescriptor->long_name)));
                }

                return Result<List<KeyValuePair<string, string>>?>.Success(infos);
            }
            catch (Exception ex)
            {
                return Result<List<KeyValuePair<string, string>>?>.Failure("FFMPEG_STREAM_CODEC_INFO_READ_FAILED", ex.Message);
            }
        }

        public static unsafe async Task<Result<List<KeyValuePair<string, string>>?>> GetVideoStreamInfos(nint mediaContextPointer, uint streamIndex)
        {
            try
            {
                AVFormatContext* formatContext = (AVFormatContext*)mediaContextPointer;
                AVStream* stream = formatContext->streams[streamIndex];
                AVCodecParameters* codecParameters = stream->codecpar;

                string prefix = $"Stream {streamIndex}";

                List<KeyValuePair<string, string>> infos =
                [
                    new KeyValuePair<string, string>($"{prefix} Width", codecParameters->width.ToString()),
            new KeyValuePair<string, string>($"{prefix} Height", codecParameters->height.ToString()),
            new KeyValuePair<string, string>($"{prefix} Pixel format", ((AVPixelFormat)codecParameters->format).ToString()),
            new KeyValuePair<string, string>($"{prefix} Average frame rate", FormatRational(stream->avg_frame_rate)),
            new KeyValuePair<string, string>($"{prefix} Real frame rate", FormatRational(stream->r_frame_rate))
                ];

                return Result<List<KeyValuePair<string, string>>?>.Success(infos);
            }
            catch (Exception ex)
            {
                return Result<List<KeyValuePair<string, string>>?>.Failure("FFMPEG_VIDEO_STREAM_INFO_READ_FAILED", ex.Message);
            }
        }

        public static unsafe async Task<Result<List<KeyValuePair<string, string>>?>> GetAudioStreamInfos(nint mediaContextPointer, uint streamIndex)
        {
            try
            {
                AVFormatContext* formatContext = (AVFormatContext*)mediaContextPointer;
                AVStream* stream = formatContext->streams[streamIndex];
                AVCodecParameters* codecParameters = stream->codecpar;

                string prefix = $"Stream {streamIndex}";

                List<KeyValuePair<string, string>> infos =
                [
                    new KeyValuePair<string, string>($"{prefix} Sample rate", codecParameters->sample_rate.ToString()),
            new KeyValuePair<string, string>($"{prefix} Channels", codecParameters->ch_layout.nb_channels.ToString()),
            new KeyValuePair<string, string>($"{prefix} Sample format", ((AVSampleFormat)codecParameters->format).ToString())
                ];

                return Result<List<KeyValuePair<string, string>>?>.Success(infos);
            }
            catch (Exception ex)
            {
                return Result<List<KeyValuePair<string, string>>?>.Failure("FFMPEG_AUDIO_STREAM_INFO_READ_FAILED", ex.Message);
            }
        }

        public static unsafe async Task<Result<bool>> CloseMediaContext(nint mediaContextPointer)
        {
            try
            {
                AVFormatContext* formatContext = (AVFormatContext*)mediaContextPointer;

                ffmpeg.avformat_close_input(&formatContext);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure("FFMPEG_CLOSE_MEDIA_CONTEXT_FAILED", ex.Message);
            }
        }




        #endregion

        #region Helpers

        private static string FormatVersion(uint version)
        {
            uint major = version >> 16;
            uint minor = (version >> 8) & 0xFF;
            uint micro = version & 0xFF;

            return $"{major}.{minor}.{micro}";
        }

        private static unsafe string PtrToString(byte* value)
        {
            if (value is null)
            {
                return string.Empty;
            }

            return Marshal.PtrToStringAnsi((IntPtr)value) ?? string.Empty;
        }

        private static string FormatDuration(long duration)
        {
            if (duration <= 0)
            {
                return string.Empty;
            }

            double seconds = duration / (double)ffmpeg.AV_TIME_BASE;

            return TimeSpan.FromSeconds(seconds).ToString();
        }

        private static string FormatRational(AVRational rational)
        {
            if (rational.den == 0)
            {
                return string.Empty;
            }

            double value = rational.num / (double)rational.den;

            return value.ToString("0.###");
        }

        private static unsafe string GetFFmpegError(int errorCode)
        {
            const int bufferSize = 1024;

            byte* buffer = stackalloc byte[bufferSize];

            ffmpeg.av_strerror(errorCode, buffer, (ulong)bufferSize);

            return PtrToString(buffer);
        }

        #endregion
    }
}