using Espluque.Contracts.Enums;
using Espluquer.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace Espluquer.Entities
{
    public class ModuleHealthDto : INotifyPropertyChanged
    {
        private string _moduleName = string.Empty;
        private ModuleHealthCheckEnum _healthCheck = ModuleHealthCheckEnum.NotTested;
        private string? _diag;

        public string ModuleName
        {
            get => _moduleName;
            set
            {
                if (_moduleName == value)
                {
                    return;
                }

                _moduleName = value;
                OnPropertyChanged();
            }
        }

        public ModuleHealthCheckEnum HealthCheck
        {
            get => _healthCheck;
            set
            {
                if (_healthCheck == value)
                {
                    return;
                }

                _healthCheck = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HealthBrush));
            }
        }

        public string? Diag
        {
            get => _diag;
            set
            {
                if (_diag == value)
                {
                    return;
                }

                _diag = value;
                OnPropertyChanged();
            }
        }

        public Brush HealthBrush => ModuleTestService.GetHealthBrush(HealthCheck);

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}