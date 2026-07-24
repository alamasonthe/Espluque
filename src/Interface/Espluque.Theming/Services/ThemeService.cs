using System.Windows;

namespace Espluque.Theming.Services
{
    public static class ThemeService
    {
        public static event Action<string>? ThemeChanged;

        public static string? CurrentTheme { get; private set; }

        public static void ApplyTheme(string themeTag)
        {
            Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/Espluque.Theming;component/Themes/{themeTag}.xaml", UriKind.Absolute)
            };

            CurrentTheme = themeTag;
            ThemeChanged?.Invoke(themeTag);
        }
    }
}