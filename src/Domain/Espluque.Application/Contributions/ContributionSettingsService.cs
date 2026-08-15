using Espluque.Contracts.ModuleInterfaces;
using Espluque.Contracts.Ports;
using System.Text.Json;

namespace Espluque.Application.Contributions
{
    /// <summary>
    /// Manages persisted user settings for contributions.
    /// </summary>
    /// <remarks>
    /// Settings are stored in the Contributions settings section.
    /// Contributions are identified by module assembly, interface type and class name.
    /// </remarks>

    public class ContributionSettingsService : IContributionSettingsService
    {
        public static string SettingsSectionName => "Contributions";

        private readonly ISettingsService _settingsService;

        public ContributionSettingsService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public async Task<List<IContributionSettingsEntry>> GetUserSettingsList()
        {
            string? settingsJsonPayload = await _settingsService.GetJsonSectionSettings(SettingsSectionName);

            if (string.IsNullOrWhiteSpace(settingsJsonPayload))
            {
                return [];
            }

            JsonSerializerOptions options = new();
            options.Converters.Add(new ContributionSettingsJsonConverter());

            List<ContributionSettingsEntry>? settings =
                JsonSerializer.Deserialize<List<ContributionSettingsEntry>>(
                    settingsJsonPayload,
                    options);

            return settings?
                .Cast<IContributionSettingsEntry>()
                .ToList()
                ?? [];
        }

        public async Task<IContributionSettings?> GetUserSettings(string moduleAssembly, string interfaceType, string className)
        {
            List<IContributionSettingsEntry> settingsList = await GetUserSettingsList();
            IContributionSettingsEntry? entry = settingsList
                .FirstOrDefault(s =>
                    s.ModuleAssembly == moduleAssembly &&
                    s.InterfaceType == interfaceType &&
                    s.ClassName == className);
            return entry?.Settings;
        }

        public async Task<bool> SaveUserSettings(string moduleAssembly, string interfaceType, string className, IContributionSettings settings)
        {
            List<IContributionSettingsEntry> settingsList = await GetUserSettingsList();
            IContributionSettingsEntry? entry = settingsList
                .FirstOrDefault(s =>
                    s.ModuleAssembly == moduleAssembly &&
                    s.InterfaceType == interfaceType &&
                    s.ClassName == className);
            if (entry != null)
            {
                entry.Settings = settings;
            }
            else
            {
                entry = new ContributionSettingsEntry
                {
                    ModuleAssembly = moduleAssembly,
                    InterfaceType = interfaceType,
                    ClassName = className,
                    Settings = settings
                };
                settingsList.Add(entry);
            }
            JsonSerializerOptions options = new();
            options.Converters.Add(new ContributionSettingsJsonConverter());
            string updatedJsonPayload = JsonSerializer.Serialize(settingsList, options);
            return await _settingsService.SaveJsonSectionSettings(SettingsSectionName, updatedJsonPayload);
        }

    }
}
