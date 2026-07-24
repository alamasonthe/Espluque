using System.Globalization;

namespace Espluque.ModuleCommons
{
    public static class GenericFormatter
    {
        public static string FormatBoolean(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            if (!bool.TryParse(value, out bool booleanValue))
            {
                return value;
            }

            return booleanValue ? "Yes" : "No";
        }

        public static string FormatBytes(string length)
        {
            if (string.IsNullOrWhiteSpace(length))
            {
                return length;
            }

            if (!long.TryParse(length, NumberStyles.Any, CultureInfo.InvariantCulture, out long bytes))
            {
                return length;
            }

            if (bytes < 1000)
            {
                return $"{bytes} bytes";
            }

            return $"{FormatBinaryBytes(bytes)} | {FormatDecimalBytes(bytes)} | {bytes} bytes";
        }


        #region Helpers

        private static string FormatBinaryBytes(long bytes)
        {
            const double kilo = 1024d;
            const double mega = kilo * 1024d;
            const double giga = mega * 1024d;

            return bytes switch
            {
                >= (long)giga => $"{(bytes / giga).ToString("0.##", CultureInfo.InvariantCulture)} GiB",
                >= (long)mega => $"{(bytes / mega).ToString("0.##", CultureInfo.InvariantCulture)} MiB",
                _ => $"{(bytes / kilo).ToString("0.##", CultureInfo.InvariantCulture)} KiB"
            };
        }

        private static string FormatDecimalBytes(long bytes)
        {
            const double kilo = 1000d;
            const double mega = kilo * 1000d;
            const double giga = mega * 1000d;

            return bytes switch
            {
                >= (long)giga => $"{(bytes / giga).ToString("0.##", CultureInfo.InvariantCulture)} GB",
                >= (long)mega => $"{(bytes / mega).ToString("0.##", CultureInfo.InvariantCulture)} MB",
                _ => $"{(bytes / kilo).ToString("0.##", CultureInfo.InvariantCulture)} kB"
            };
        }

        #endregion
    }
}
