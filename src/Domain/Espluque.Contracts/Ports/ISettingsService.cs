namespace Espluque.Contracts.Ports;

public interface ISettingsService
{
    Task<string?> GetSetting(string key);
    Task<bool> SaveSetting(string key, string value);
}