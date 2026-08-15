using Espluque.Contracts.Contributions.Types;
using Espluque.Contracts.CrossCutting;
using Espluque.Contracts.Workflow;
using ImageMagick;
using Microsoft.Win32;
using System.Globalization;
using Util;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Magick
{
    internal class Grabber: IGrabber
    {
        private readonly IMessageCenter _messageCenter;
        private readonly ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        public Grabber(IMessageCenter messageCenter,
            ILogger logger,
            ISettingsService settingsService,
            IEntityFactory entityFactory)
        {
            _messageCenter = messageCenter;
            _logger = logger;
            _settingsService = settingsService;
            _entityFactory = entityFactory;
        }

        public async Task<List<KeyValuePair<string, string>>> Grab(IAnalysisContext analysisContext)
        {
            try
            {
                using MagickImage image = new(analysisContext.FilePath);

                List<KeyValuePair<string, string>> properties =
                [
                    new("Format", image.Format.ToString()),
                    new("Width", image.Width.ToString(CultureInfo.InvariantCulture)),
                    new("Height", image.Height.ToString(CultureInfo.InvariantCulture)),
                    new("Base width", image.BaseWidth.ToString(CultureInfo.InvariantCulture)),
                    new("Base height", image.BaseHeight.ToString(CultureInfo.InvariantCulture)),
                    new("Color space", image.ColorSpace.ToString()),
                    new("Color type", image.ColorType.ToString()),
                    new("Depth", image.Depth.ToString(CultureInfo.InvariantCulture)),
                    new("Has alpha", image.HasAlpha.ToString()),
                    new("Is opaque", image.IsOpaque.ToString()),
                    new("Compression", image.Compression.ToString()),
                    new("Interlace", image.Interlace.ToString()),
                    new("Orientation", image.Orientation.ToString()),
                    new("Quality", image.Quality.ToString(CultureInfo.InvariantCulture)),
                    new("Gamma", image.Gamma.ToString(CultureInfo.InvariantCulture)),
                    new("Channel count", image.ChannelCount.ToString(CultureInfo.InvariantCulture)),
                    new("Channels", string.Join(", ", image.Channels)),
                    new("Profiles", string.Join(", ", image.ProfileNames))
                ];

                properties.AddRange(GetExifProperties(image));

                return new Formatter().Format(properties);
            }
            catch (Exception ex)
            {
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"ImageMagick Images property Reader: {ex.Message}");
                return new List<KeyValuePair<string, string>>();
            }
        }

        private static List<KeyValuePair<string, string>> GetExifProperties(MagickImage image)
        {
            List<KeyValuePair<string, string>> properties = [];

            IExifProfile? profile = image.GetExifProfile();

            if (profile is null)
            {
                return properties;
            }

            foreach (IExifValue value in profile.Values)
            {
                string formattedValue = value.ToString();

                if (formattedValue.StartsWith("ImageMagick.Exif", StringComparison.Ordinal))
                {
                    continue;
                }

                properties.Add(new KeyValuePair<string, string>(
                    $"EXIF {value.Tag}",
                    formattedValue));
            }

            properties.AddRange(GetGpsProperties(profile));

            return properties;
        }

        private static List<KeyValuePair<string, string>> GetGpsProperties(IExifProfile profile)
        {
            var gpsVersion = profile.GetValue(ExifTag.GPSVersionID)?.Value;
            var latitude = profile.GetValue(ExifTag.GPSLatitude)?.Value;
            var latitudeRef = profile.GetValue(ExifTag.GPSLatitudeRef)?.Value;
            var longitude = profile.GetValue(ExifTag.GPSLongitude)?.Value;
            var longitudeRef = profile.GetValue(ExifTag.GPSLongitudeRef)?.Value;
            var altitudeRef = profile.GetValue(ExifTag.GPSAltitudeRef)?.Value;
            var altitude = profile.GetValue(ExifTag.GPSAltitude)?.Value;

            List<KeyValuePair<string, string>> GpsProperties = [];

            if (gpsVersion is not null)
            {
                GpsProperties.Add(new KeyValuePair<string, string>("GPS Version ID", string.Join(".", gpsVersion.Select(value => value.ToString(CultureInfo.InvariantCulture)))));
            }

            if (!string.IsNullOrWhiteSpace(latitudeRef))
            {
                GpsProperties.Add(new KeyValuePair<string, string>("GPS Latitude Ref", latitudeRef));
            }

            if (latitude is not null)
            {
                GpsProperties.Add(new KeyValuePair<string, string>("GPS Latitude", string.Join(", ", latitude.Select(value => value.ToString()))));
            }

            if (!string.IsNullOrWhiteSpace(longitudeRef))
            {
                GpsProperties.Add(new KeyValuePair<string, string>("GPS Longitude Ref", longitudeRef));
            }

            if (longitude is not null)
            {
                GpsProperties.Add(new KeyValuePair<string, string>("GPS Longitude", string.Join(", ", longitude.Select(value => value.ToString()))));
            }

            if (altitudeRef is byte altitudeRefValue)
            {
                GpsProperties.Add(new KeyValuePair<string, string>("GPS Altitude Ref", altitudeRefValue.ToString(CultureInfo.InvariantCulture)));
            }

            if (altitude is Rational altitudeValue)
            {
                GpsProperties.Add(new KeyValuePair<string, string>("GPS Altitude", altitudeValue.ToString()));
            }

            return GpsProperties;
        }

    }
}
