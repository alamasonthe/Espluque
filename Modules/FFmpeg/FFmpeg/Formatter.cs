using Espluque.ModuleCommons;
using System.Globalization;

namespace FFmpeg
{
    internal class Formatter : FormatterBase
    {
        private readonly List<string> _ignoredKeys =
        [
            "Format long name",
            "Stream 0 Codec long name",
            "Stream 1 Codec long name",
            "Stream 2 Codec long name",
            "Stream 3 Codec long name",
            "Stream 4 Codec long name",
            "Stream 5 Codec long name",
        ];

        public override KeyValuePair<string, string>? Format(KeyValuePair<string, string> item)
        {
            if (_ignoredKeys.Contains(item.Key))
            {
                return null;
            }

            string value = item.Value;
            switch (item.Key)
            {
                case "Format name":
                    value = FormatFormatName(value);
                    break;

                case "Duration":
                    value = FormatDuration(value);
                    break;

                case "Bit rate":
                    value = FormatBitRate(value);
                    break;

            }

            if (item.Key.EndsWith(" frame rate", StringComparison.Ordinal))
            {
                value = FormatFrameRate(value);
            }

            if (item.Key.EndsWith(" Codec", StringComparison.Ordinal))
            {
                value = FormatCodec(value);
            }

            if (item.Key.EndsWith(" Type", StringComparison.Ordinal))
            {
                value = FormatStreamType(value);
            }

            if (item.Key.EndsWith(" Pixel format", StringComparison.Ordinal))
            {
                value = FormatPixelFormat(value);
            }

            if (item.Key.EndsWith(" Sample format", StringComparison.Ordinal))
            {
                value = FormatSampleFormat(value);
            }

            KeyValuePair<string, string> newKeyValuePair = new(item.Key, value);
            return newKeyValuePair;
        }

        private string FormatFormatName(string formatName)
        {
            if (string.IsNullOrWhiteSpace(formatName))
            {
                return formatName;
            }

            return formatName switch
            {
                "image2" => "Image file / image sequence",
                "mjpeg" => "Raw MJPEG video",
                "gif" => "GIF image / animation",
                "apng" => "Animated PNG",
                "mp3" => "MP3 audio",
                "wav" => "WAV audio",
                "flac" => "FLAC audio",
                "ogg" => "Ogg container",
                "avi" => "AVI container",
                "matroska,webm" => "Matroska / WebM container",
                "mov,mp4,m4a,3gp,3g2,mj2" => "QuickTime / MP4 container",
                "mpeg" => "MPEG program stream",
                "mpegts" => "MPEG transport stream",
                "asf" => "ASF / WMV container",
                _ => formatName
            };
        }

        private string FormatDuration(string duration)
        {
            if (string.IsNullOrWhiteSpace(duration))
            {
                return duration;
            }

            if (!TimeSpan.TryParse(duration, CultureInfo.InvariantCulture, out TimeSpan timeSpan))
            {
                return duration;
            }

            if (timeSpan.TotalMilliseconds < 1000)
            {
                return $"{timeSpan.TotalMilliseconds.ToString("0.##", CultureInfo.InvariantCulture)} ms";
            }

            return timeSpan.ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture);
        }

        private string FormatBitRate(string bitRate)
        {
            if (string.IsNullOrWhiteSpace(bitRate))
            {
                return bitRate;
            }

            if (!long.TryParse(bitRate, NumberStyles.Any, CultureInfo.InvariantCulture, out long bitsPerSecond))
            {
                return bitRate;
            }

            double megabitsPerSecond = bitsPerSecond / 1_000_000d;

            return $"{megabitsPerSecond.ToString("0.##", CultureInfo.InvariantCulture)} Mb/s";
        }

        private string FormatFrameRate(string frameRate)
        {
            if (string.IsNullOrWhiteSpace(frameRate))
            {
                return frameRate;
            }

            if (!TryParseRational(frameRate, out double fps))
            {
                return $"{frameRate} fps";
            }

            return $"{fps.ToString("0.###", CultureInfo.InvariantCulture)} fps";
        }

        #region Helpers

        private bool TryParseRational(string value, out double result)
        {
            result = 0;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string[] parts = value.Split('/');

            if (parts.Length == 1)
            {
                return double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out result);
            }

            if (parts.Length != 2)
            {
                return false;
            }

            bool numeratorParsed = double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double numerator);
            bool denominatorParsed = double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double denominator);

            if (!numeratorParsed || !denominatorParsed || denominator == 0)
            {
                return false;
            }

            result = numerator / denominator;
            return true;
        }

        private string FormatCodec(string codec)
        {
            if (string.IsNullOrWhiteSpace(codec))
            {
                return codec;
            }

            return codec switch
            {
                "mjpeg" => "Motion JPEG",
                _ => codec
            };
        }

        private string FormatStreamType(string streamType)
        {
            if (string.IsNullOrWhiteSpace(streamType))
            {
                return streamType;
            }

            return streamType switch
            {
                "AVMEDIA_TYPE_VIDEO" => "Video",
                "AVMEDIA_TYPE_AUDIO" => "Audio",
                "AVMEDIA_TYPE_SUBTITLE" => "Subtitle",
                "AVMEDIA_TYPE_DATA" => "Data",
                "AVMEDIA_TYPE_ATTACHMENT" => "Attachment",
                "AVMEDIA_TYPE_UNKNOWN" => "Unknown",
                _ => streamType
            };
        }

        private string FormatPixelFormat(string pixelFormat)
        {
            if (string.IsNullOrWhiteSpace(pixelFormat))
            {
                return pixelFormat;
            }

            return pixelFormat switch
            {
                "AV_PIX_FMT_YUVJ444P" => "YUVJ444P (YUV 4:4:4 planar, full range)",
                "AV_PIX_FMT_YUVJ422P" => "YUVJ422P (YUV 4:2:2 planar, full range)",
                "AV_PIX_FMT_YUVJ420P" => "YUVJ420P (YUV 4:2:0 planar, full range)",
                "AV_PIX_FMT_YUV444P" => "YUV444P (YUV 4:4:4 planar)",
                "AV_PIX_FMT_YUV422P" => "YUV422P (YUV 4:2:2 planar)",
                "AV_PIX_FMT_YUV420P" => "YUV420P (YUV 4:2:0 planar)",
                "AV_PIX_FMT_RGB24" => "RGB24",
                "AV_PIX_FMT_BGR24" => "BGR24",
                "AV_PIX_FMT_RGBA" => "RGBA",
                "AV_PIX_FMT_BGRA" => "BGRA",
                _ => pixelFormat.Replace("AV_PIX_FMT_", string.Empty)
            };
        }

        private string FormatSampleFormat(string sampleFormat)
        {
            if (string.IsNullOrWhiteSpace(sampleFormat))
            {
                return sampleFormat;
            }

            return sampleFormat switch
            {
                "AV_SAMPLE_FMT_U8" => "Unsigned 8-bit",
                "AV_SAMPLE_FMT_S16" => "Signed 16-bit",
                "AV_SAMPLE_FMT_S32" => "Signed 32-bit",
                "AV_SAMPLE_FMT_FLT" => "Float",
                "AV_SAMPLE_FMT_DBL" => "Double",
                "AV_SAMPLE_FMT_U8P" => "Unsigned 8-bit planar",
                "AV_SAMPLE_FMT_S16P" => "Signed 16-bit planar",
                "AV_SAMPLE_FMT_S32P" => "Signed 32-bit planar",
                "AV_SAMPLE_FMT_FLTP" => "Float planar",
                "AV_SAMPLE_FMT_DBLP" => "Double planar",
                "AV_SAMPLE_FMT_S64" => "Signed 64-bit",
                "AV_SAMPLE_FMT_S64P" => "Signed 64-bit planar",
                _ => sampleFormat.Replace("AV_SAMPLE_FMT_", string.Empty)
            };
        }

        #endregion
    }
}
