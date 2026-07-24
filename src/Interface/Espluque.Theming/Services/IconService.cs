using System.IO;
using System.Text.Json;
using System.Windows.Resources;
using System.Windows;

namespace Espluque.Theming.Services
{
    public class IconService
    {
        private static readonly Dictionary<string, int> FluentIconMap = LoadFluentIconMap();

        public static Dictionary<string, int> LoadFluentIconMap()
        {
            Uri uri = new(
                "pack://application:,,,/Espluque.Theming;component/Fonts/FluentSystemIcons-Regular.json",
                UriKind.Absolute);
            StreamResourceInfo? resourceInfo = Application.GetResourceStream(uri);

            if (resourceInfo == null)
            {
                throw new FileNotFoundException("FluentSystemIcons-Regular.json introuvable. Vérifie le chemin et Build Action = Resource.");
            }

            using Stream stream = resourceInfo.Stream;

            return JsonSerializer.Deserialize<Dictionary<string, int>>(stream)
                ?? throw new InvalidOperationException("Le mapping Fluent Icons est vide ou invalide.");
        }

        public static string FluentGlyph(string iconName)
        {
            if (!FluentIconMap.TryGetValue(iconName, out int codePoint))
            {
                throw new KeyNotFoundException($"Icône Fluent introuvable : {iconName}");
            }

            return char.ConvertFromUtf32(codePoint);
        }

        private static string GetFluentIconCode(string iconName)
        {
            int codePoint = FluentIconMap[iconName];
            return $"U+{codePoint:X}";
        }
    }
}
