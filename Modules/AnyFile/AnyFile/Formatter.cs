using Espluque.ModuleCommons;

namespace AnyFile
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
                case "IsReadOnly":
                    value = GenericFormatter.FormatBoolean(value);
                    break;

                case "Length":
                    value = GenericFormatter.FormatBytes(value);
                    break;
            }

            KeyValuePair<string, string> newKeyValuePair = new(item.Key, value);
            return newKeyValuePair;
        }
    }
}
