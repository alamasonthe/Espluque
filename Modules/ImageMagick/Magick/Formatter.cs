using Espluque.ModuleCommons;
using System.Globalization;

namespace Magick
{
    internal class Formatter: FormatterBase
    {
        private readonly List<string> _ignoredKeys =
        [
            "Base width",
            "Base height",
            "Compression",
            "EXIF ColorSpace",
            "EXIF ResolutionUnit",
            "EXIF ShutterSpeedValue",
            "EXIF ApertureValue",
            "EXIF GPSLatitudeRef",
            "EXIF GPSLongitudeRef",
            "EXIF GPSAltitudeRef",
            "EXIF GPSAltitude"
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
                case "Depth":
                    value = FormatDepth(value);
                    break;

                case "Quality":
                    value = FormatPercent(value);
                    break;

                case "EXIF FNumber":
                    value = FormatFNumber(value);
                    break;

                case "GPS Altitude Ref":
                    value = FormatGpsAltitudeRef(value);
                    break;

                case "GPS Latitude":
                case "GPS Longitude":
                    value = FormatGpsCoordinate(value);
                    break;

                case "GPS Version ID":
                    value = FormatGpsVersion(value);
                    break;

                case "EXIF FocalLength":
                    value = FormatMillimeters(value);
                    break;

                case "EXIF FocalLengthIn35mmFilm":
                    value = FormatMillimeters(value);
                    break;

                case "EXIF SceneType":
                    value = FormatSceneType(value);
                    break;

                case "GPS Altitude":
                    value = FormatGpsAltitude(value);
                    break;

                case "Gamma":
                    value = FormatGamma(value);
                    break;

                case "EXIF BrightnessValue":
                    value = FormatBrightnessValue(value);
                    break;

                case "EXIF ExposureBiasValue":
                    value = FormatExposureBiasValue(value);
                    break;

                case "EXIF MaxApertureValue":
                    value = FormatApertureValue(value);
                    break;

                case "EXIF XResolution":
                case "EXIF YResolution":
                    value = FormatDpi(value);
                    break;

                case "EXIF ExposureTime":
                    value = FormatExposureTime(value);
                    break;

                case "Interlace":
                    value = FormatInterlace(value);
                    break;

                case "Has alpha":
                case "Is opaque":
                    value = GenericFormatter.FormatBoolean(value);
                    break;
            }

            KeyValuePair<string, string> newKeyValuePair = new(item.Key, value);
            return newKeyValuePair;
        }

        private string FormatFNumber(string fNumber)
        {
            if (string.IsNullOrWhiteSpace(fNumber))
            {
                return string.Empty;
            }

            string[] parts = fNumber.Split('/');

            if (parts.Length != 2)
            {
                return fNumber;
            }

            bool numeratorParsed = double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double numerator);
            bool denominatorParsed = double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double denominator);

            if (!numeratorParsed || !denominatorParsed || denominator == 0)
            {
                return fNumber;
            }

            double aperture = numerator / denominator;

            return $"f/{aperture.ToString("0.#", CultureInfo.InvariantCulture)}";
        }

        private string FormatDepth(string depth)
        {
            if (string.IsNullOrWhiteSpace(depth))
            {
                return depth;
            }

            return $"{depth} bits";
        }

        private string FormatPercent(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return $"{value} %";
        }

        private string FormatGpsAltitude(string altitude)
        {
            if (string.IsNullOrWhiteSpace(altitude))
            {
                return altitude;
            }

            string[] parts = altitude.Split('/');

            if (parts.Length != 2)
            {
                return $"{altitude} m";
            }

            bool numeratorParsed = double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double numerator);
            bool denominatorParsed = double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double denominator);

            if (!numeratorParsed || !denominatorParsed || denominator == 0)
            {
                return $"{altitude} m";
            }

            double metres = numerator / denominator;

            return $"{metres.ToString("0.##", CultureInfo.InvariantCulture)} m";
        }

        private string FormatGpsAltitudeRef(string altitudeRef)
        {
            if (string.IsNullOrWhiteSpace(altitudeRef))
            {
                return altitudeRef;
            }

            return altitudeRef switch
            {
                "0" => "Sea level",
                "00" => "Sea level",
                "1" => "Below sea level",
                "01" => "Below sea level",
                _ => altitudeRef
            };
        }

        private string FormatGpsCoordinate(string coordinate)
        {
            if (string.IsNullOrWhiteSpace(coordinate))
            {
                return coordinate;
            }

            string[] parts = coordinate.Split(',', StringSplitOptions.TrimEntries);

            if (parts.Length != 3)
            {
                return coordinate;
            }

            if (!TryParseRational(parts[0], out double degrees))
            {
                return coordinate;
            }

            if (!TryParseRational(parts[1], out double minutes))
            {
                return coordinate;
            }

            if (!TryParseRational(parts[2], out double seconds))
            {
                return coordinate;
            }

            double wholeMinutes = Math.Truncate(minutes);
            double remainingSeconds = ((minutes - wholeMinutes) * 60) + seconds;

            return $"{degrees:0}° {wholeMinutes:0}' {remainingSeconds:0.##}\"";
        }

        private string FormatGpsVersion(string gpsVersion)
        {
            if (string.IsNullOrWhiteSpace(gpsVersion))
            {
                return gpsVersion;
            }

            string[] parts = gpsVersion.Split('.', StringSplitOptions.TrimEntries);

            if (parts.Length < 4)
            {
                return gpsVersion;
            }

            return $"{parts[0]}.{parts[1]}{parts[2]}{parts[3]}";
        }

        private string FormatMillimeters(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            if (!TryParseRational(value, out double millimeters))
            {
                return $"{value} mm";
            }

            return $"{millimeters.ToString("0.##", CultureInfo.InvariantCulture)} mm";
        }

        private string FormatSceneType(string sceneType)
        {
            if (string.IsNullOrWhiteSpace(sceneType))
            {
                return sceneType;
            }

            return sceneType switch
            {
                "1" => "Directly photographed",
                "01" => "Directly photographed",
                _ => sceneType
            };
        }

        private string FormatGamma(string gamma)
        {
            if (string.IsNullOrWhiteSpace(gamma))
            {
                return gamma;
            }

            if (!double.TryParse(gamma, NumberStyles.Any, CultureInfo.InvariantCulture, out double gammaValue))
            {
                return gamma;
            }

            return gammaValue.ToString("0.####", CultureInfo.InvariantCulture);
        }

        private string FormatBrightnessValue(string brightnessValue)
        {
            if (string.IsNullOrWhiteSpace(brightnessValue))
            {
                return brightnessValue;
            }

            if (!TryParseRational(brightnessValue, out double brightness))
            {
                return brightnessValue;
            }

            return brightness.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private string FormatExposureBiasValue(string exposureBiasValue)
        {
            if (string.IsNullOrWhiteSpace(exposureBiasValue))
            {
                return exposureBiasValue;
            }

            if (!TryParseRational(exposureBiasValue, out double exposureBias))
            {
                return exposureBiasValue;
            }

            return $"{exposureBias.ToString("0.##", CultureInfo.InvariantCulture)} EV";
        }

        private string FormatApertureValue(string apertureValue)
        {
            if (string.IsNullOrWhiteSpace(apertureValue))
            {
                return apertureValue;
            }

            if (!TryParseRational(apertureValue, out double apexValue))
            {
                return apertureValue;
            }

            double aperture = Math.Pow(2, apexValue / 2);

            return $"f/{aperture.ToString("0.#", CultureInfo.InvariantCulture)}";
        }

        private string FormatDpi(string resolution)
        {
            if (string.IsNullOrWhiteSpace(resolution))
            {
                return resolution;
            }

            if (!TryParseRational(resolution, out double dpi))
            {
                return $"{resolution} DPI";
            }

            return $"{dpi.ToString("0.##", CultureInfo.InvariantCulture)} DPI";
        }

        private string FormatExposureTime(string exposureTime)
        {
            if (string.IsNullOrWhiteSpace(exposureTime))
            {
                return exposureTime;
            }

            return $"{exposureTime} sec";
        }

        private string FormatInterlace(string interlace)
        {
            if (string.IsNullOrWhiteSpace(interlace))
            {
                return interlace;
            }

            return interlace switch
            {
                "NoInterlace" => "No interlace",
                "LineInterlace" => "Line interlace",
                "PlaneInterlace" => "Plane interlace",
                "PartitionInterlace" => "Partition interlace",
                _ => interlace
            };
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

        #endregion
    }
}
