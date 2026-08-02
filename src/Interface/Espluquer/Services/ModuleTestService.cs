using Espluque.Contracts.Enums;
using Espluque.Theming.Services;

namespace Espluquer.Services
{
    public static class ModuleTestService
    {
        private const string DefaultContributionIconName = "ic_fluent_question_circle_24_regular";

        private const string DefaultContributionColorKey = "App.TextMuted";
        private const string UnknownContributionColorKey = "App.StatusWarning";

        private static readonly Dictionary<string, string> ContributionIconNames = new(StringComparer.Ordinal)
            {
                ["IDetector"] = "ic_fluent_search_24_regular",
                ["IGrabber"] = "ic_fluent_clipboard_data_bar_24_regular",
                ["IFusioner"] = "ic_fluent_merge_24_regular",
                ["IWpfViewer"] = "ic_fluent_eye_24_regular",
                ["IWpfSettings"] = "ic_fluent_settings_24_regular",
                ["IWpfMaintenance"] = "ic_fluent_wrench_screwdriver_24_regular",
                ["IManagedDependencies"] = "ic_fluent_cube_multiple_24_regular"
        };

        private static readonly Dictionary<ModuleHealthCheckEnum, string> ContributionColorKeys = new()
            {
                [ModuleHealthCheckEnum.Success] = "App.StatusSuccess",
                [ModuleHealthCheckEnum.Running] = "App.StatusWarning",
                [ModuleHealthCheckEnum.Error] = "App.StatusError",
                [ModuleHealthCheckEnum.NotTested] = "App.TextMuted"
            };

        public static string GetContributionIcon(string interfaceType)
        {
            string iconName = DefaultContributionIconName;

            if (ContributionIconNames.TryGetValue( interfaceType, out string? mappedIconName))
            {
                iconName = mappedIconName;
            }

            return IconService.FluentGlyph(iconName);
        }

        public static string GetContributionColorKey(string interfaceType, ModuleHealthCheckEnum healthCheck)
        {
            if (!ContributionIconNames.ContainsKey(interfaceType))
            {
                return UnknownContributionColorKey;
            }

            if (ContributionColorKeys.TryGetValue(healthCheck, out string? colorKey))
            {
                return colorKey;
            }

            return DefaultContributionColorKey;
        }
    }
}