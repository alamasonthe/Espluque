namespace Espluque.Contracts.Contributions
{
    public interface IContributionSettingsService
    {
        Task<IContributionSettings?> GetUserSettings(string moduleAssembly, string interfaceType, string className);
        Task<List<IContributionSettingsEntry>> GetUserSettingsList();
        Task<bool> SaveUserSettings(string moduleAssembly, string interfaceType, string className, IContributionSettings settings);
    }
}