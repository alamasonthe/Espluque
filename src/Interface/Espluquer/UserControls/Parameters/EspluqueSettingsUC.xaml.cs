using Espluque.Contracts.Ports;
using Espluquer.Entities;
using System.Windows;
using System.Windows.Controls;

namespace Espluquer.UserControls.Parameters
{
    public partial class EspluqueSettingsUC : UserControl
    {
        private const string _moduleName = "Espluquer";

        private readonly ISettingsService _settingsService;
        private string _settingsFilePath = string.Empty;
        private SettingsDto _settingsDto = new SettingsDto();

        public EspluqueSettingsUC(ISettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService;

            _settingsFilePath = _settingsService.GetSettingsFilePath() ?? string.Empty;

            DataContext = _settingsDto;

        }

        private async Task LoadSettings()
        {
            string? moduleSettingsJson = await _settingsService.GetModuleSettings(_moduleName);
            if (string.IsNullOrWhiteSpace(moduleSettingsJson))
            {
                return;
            }
            _settingsDto = System.Text.Json.JsonSerializer.Deserialize<SettingsDto>(moduleSettingsJson) ?? new SettingsDto();
        }

        private async void SaveButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            SaveStatusTextBlock.Text = string.Empty;

            bool dbSaved = await _settingsService.SaveSetting( _moduleName, nameof(SettingsDto.Db), _settingsDto.Db);

            bool logFilePathSaved = await _settingsService.SaveSetting( _moduleName, nameof(SettingsDto.LogFilePath), _settingsDto.LogFilePath);

            SaveStatusTextBlock.Text = dbSaved && logFilePathSaved ? "Settings saved." : "Unable to save settings.";
        }
    }
}
