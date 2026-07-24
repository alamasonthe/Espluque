using Espluque.ModuleCommons;

namespace Shortcut
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
                case "Show Command":
                    value = FormatShowCommand(value);
                    break;

            }

            KeyValuePair<string, string> newKeyValuePair = new(item.Key, value);
            return newKeyValuePair;
        }

        private string FormatShowCommand(string showCommand)
        {
            if (string.IsNullOrWhiteSpace(showCommand))
            {
                return showCommand;
            }

            return showCommand switch
            {
                "1" => "Normal window",
                "3" => "Maximized",
                "7" => "Minimized",
                _ => showCommand
            };
        }
    }
}
