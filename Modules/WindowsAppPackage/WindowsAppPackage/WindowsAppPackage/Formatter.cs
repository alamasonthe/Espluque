using Espluque.ModuleCommons;

namespace WindowsAppPackage
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
                case "DeclaredContentSize":
                    value = GenericFormatter.FormatBytes(value);
                    break;
            }

            KeyValuePair<string, string> newKeyValuePair = new(item.Key, value);
            return newKeyValuePair;
        }
    }
}
