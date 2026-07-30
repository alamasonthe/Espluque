using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Espluquer.Entities
{
    internal class SettingsDto : INotifyPropertyChanged
    {
        private string _db = string.Empty;
        private string _logFilePath = string.Empty;
        private string _theme = string.Empty;
        private string _recentFiles = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Db
        {
            get => _db;
            set => SetField(ref _db, value);
        }

        public string LogFilePath
        {
            get => _logFilePath;
            set => SetField(ref _logFilePath, value);
        }

        public string Theme
        {
            get => _theme;
            set => SetField(ref _theme, value);
        }

        public string RecentFiles
        {
            get => _recentFiles;
            set
            {
                if (SetField(ref _recentFiles, value))
                {
                    OnPropertyChanged(nameof(RecentFilesText));
                }
            }
        }

        [JsonIgnore]
        public string RecentFilesText
        {
            get
            {
                return string.Join( Environment.NewLine,
                    RecentFiles.Split( '|', StringSplitOptions.RemoveEmptyEntries));
            }

            set
            {
                RecentFiles = string.Join( "|",
                    value.Split( ["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries));
            }
        }

        private bool SetField( ref string field, string value, [CallerMemberName] string? propertyName = null)
        {
            if (field == value)
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);

            return true;
        }

        private void OnPropertyChanged(string? propertyName)
        {
            PropertyChanged?.Invoke( this, new PropertyChangedEventArgs(propertyName));
        }
    }
}