using Espluque.Contracts.Ports;
using Espluquer.Entities;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Espluquer.UserControls.Espluque
{
    public partial class EspluqueSettingsUC : UserControl
    {
        private const string _moduleName = "Espluquer";

        private readonly ILogger _logger;
        private readonly ISettingsService _settingsService;
        private string _settingsFilePath = string.Empty;
        private SettingsDto _settingsDto = new SettingsDto();

        public EspluqueSettingsUC(ILogger logger, ISettingsService settingsService)
        {
            InitializeComponent();
            _logger = logger;
            _settingsService = settingsService;

            _settingsFilePath = _settingsService.GetSettingsFilePath() ?? string.Empty;
            FilePathTextbox.Text = _settingsFilePath;
            LoadSettings();

        }

        private async Task LoadSettings()
        {
            string? moduleSettingsJson = await _settingsService.GetJsonSectionSettings(_moduleName);
            if (string.IsNullOrWhiteSpace(moduleSettingsJson))
            {
                return;
            }
            _settingsDto = System.Text.Json.JsonSerializer.Deserialize<SettingsDto>(moduleSettingsJson) ?? new SettingsDto();

            DataContext = _settingsDto;

            _settingsDto.PropertyChanged += SettingsDto_PropertyChanged;
        }

        private async void SettingsDto_PropertyChanged( object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SettingsDto.RecentFilesText))
            {
                return;
            }

            bool dbSaved = await _settingsService.SaveSetting( _moduleName, nameof(SettingsDto.Db), _settingsDto.Db);
            bool logFilePathSaved = await _settingsService.SaveSetting( _moduleName, nameof(SettingsDto.LogFilePath), _settingsDto.LogFilePath);
            bool themeSaved = await _settingsService.SaveSetting( _moduleName, nameof(SettingsDto.Theme), _settingsDto.Theme);
            bool recentFilesSaved = await _settingsService.SaveSetting( _moduleName, nameof(SettingsDto.RecentFiles), _settingsDto.RecentFiles);

            if (dbSaved && logFilePathSaved && themeSaved && recentFilesSaved)
            {
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Debug, $"Settings saved.");
            }
            else
            {
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Warning, $"Error saving settings.");
            }
        }

        private void OpenFileLocationButton_Click(object sender, RoutedEventArgs e)
        {
            if (!System.IO.File.Exists(_settingsFilePath))
            {
                return;
            }

            string arguments = $"/select,\"{_settingsFilePath}\"";

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = arguments,
                UseShellExecute = true
            });
        }

    }
}
