using Espluque.Contracts.Ports;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SettingsJson
{
    public class SettingsService : ISettingsService
    {
        public string? GetSettingsFilePath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appName = Assembly.GetEntryAssembly()?.GetName().Name ?? Process.GetCurrentProcess().ProcessName;

            string filePathCandidate1 = Path.Combine(appDataPath, appName, "settings.json");
            if (File.Exists(filePathCandidate1))
            {
                return filePathCandidate1;
            }

            string appFolderPath = AppContext.BaseDirectory;

            string filePathCandidate2 = Path.Combine(appFolderPath, "settings.json");
            if (File.Exists(filePathCandidate2))
            {
                return filePathCandidate2;
            }

            return null;
        }

        private string GetWriteSettingsFilePath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appName = Assembly.GetEntryAssembly()?.GetName().Name ?? Process.GetCurrentProcess().ProcessName;

            return Path.Combine(appDataPath, appName, "settings.json");
        }

        public Task<string?> GetModuleSettings(string moduleName)
        {
            try
            {
                string? settingsFilePath = GetSettingsFilePath();

                if (string.IsNullOrWhiteSpace(settingsFilePath))
                {
                    return Task.FromResult<string?>(null);
                }

                string json = File.ReadAllText(settingsFilePath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    return Task.FromResult<string?>(null);
                }

                using JsonDocument document = JsonDocument.Parse(json);

                if (!document.RootElement.TryGetProperty(moduleName, out JsonElement moduleSettings))
                {
                    return Task.FromResult<string?>(null);
                }

                string moduleSettingsJson = moduleSettings.GetRawText();

                return Task.FromResult<string?>(moduleSettingsJson);
            }
            catch
            {
                return Task.FromResult<string?>(null);
            }
        }

        public async Task<string?> GetSetting(string moduleName, string key)
        {
            var moduleJson = await GetModuleSettings(moduleName);
            if (string.IsNullOrWhiteSpace(moduleJson))
            {
                return null;
            }

            using JsonDocument document = JsonDocument.Parse(moduleJson);

            if (!document.RootElement.TryGetProperty(key, out JsonElement settingValue))
            {
                return null;
            }

            if (settingValue.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            string? value = settingValue.GetString();

            return value;
        }

        public async Task<string?> GetSetting(string key)
        {
            string appName = Assembly.GetEntryAssembly()?.GetName().Name ?? Process.GetCurrentProcess().ProcessName;

            return await GetSetting(appName, key);
        }

        public async Task<bool> SaveSetting(string moduleName, string key, string value)
        {
            var jsonFilePath = GetWriteSettingsFilePath();

            try
            {
                string? directoryPath = Path.GetDirectoryName(jsonFilePath);

                if (!string.IsNullOrWhiteSpace(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                JsonObject rootObject;

                if (File.Exists(jsonFilePath))
                {
                    string json = await File.ReadAllTextAsync(jsonFilePath);

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        rootObject = new JsonObject();
                    }
                    else
                    {
                        JsonNode? rootNode = JsonNode.Parse(json);

                        if (rootNode is not JsonObject existingRootObject)
                        {
                            return false;
                        }

                        rootObject = existingRootObject;
                    }
                }
                else
                {
                    rootObject = new JsonObject();
                }

                JsonObject moduleObject;

                if (rootObject.TryGetPropertyValue(moduleName, out JsonNode? moduleNode))
                {
                    if (moduleNode is not JsonObject existingModuleObject)
                    {
                        return false;
                    }

                    moduleObject = existingModuleObject;
                }
                else
                {
                    moduleObject = new JsonObject();
                    rootObject[moduleName] = moduleObject;
                }

                moduleObject[key] = value;

                string updatedJson = rootObject.ToJsonString(new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(jsonFilePath, updatedJson);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SaveSetting(string key, string value)
        {
            string appName = Assembly.GetEntryAssembly()?.GetName().Name ?? Process.GetCurrentProcess().ProcessName;
            return await SaveSetting(appName, key, value);
        }
    }
}
