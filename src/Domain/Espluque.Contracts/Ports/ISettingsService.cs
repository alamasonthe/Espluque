namespace Espluque.Contracts.Ports;

public interface ISettingsService
{
    string? GetSettingsFilePath();

    Task<string?> GetSetting(string key);
    Task<string?> GetSetting(string moduleName, string key);

    Task<bool> SaveSetting(string key, string value);
    Task<bool> SaveSetting(string moduleName, string key, string value);

    Task<string?> GetModuleSettings(string moduleName);
}