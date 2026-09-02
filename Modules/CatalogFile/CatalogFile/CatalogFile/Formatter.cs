using Espluque.ModuleCommons;

namespace CatalogFile
{
    internal class Formatter : FormatterBase
    {
        private readonly List<string> _ignoredKeys =
        [];

        public override KeyValuePair<string, string>? Format(KeyValuePair<string, string> item)
        {
            if (_ignoredKeys.Contains(item.Key))
            {
                return null;
            }

            string value = item.Value;

            switch (item.Key)
            {
                case "OSAttr":
                    value = FormatOSAttr(value);
                    break;
            }

            return new KeyValuePair<string, string>(item.Key, value);
        }

        private static string FormatOSAttr(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            string[] parts = value.Split(':');

            if (parts.Length != 2)
            {
                return value;
            }

            string osFamily = parts[0] switch
            {
                "2" => "Windows NT",
                _ => $"Unknown OS family ({parts[0]})"
            };

            return $"{osFamily} {parts[1]}";
        }
    }
}
