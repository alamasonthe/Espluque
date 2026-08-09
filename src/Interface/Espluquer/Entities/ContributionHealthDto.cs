using Espluque.Contracts.Enums;
using Espluquer.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace Espluquer.Entities
{
    public class ContributionHealthDto : INotifyPropertyChanged
    {
        public string ModuleName { get; set; } = string.Empty;
        public string ContribInterfaceType { get; set; } = string.Empty;
        public string ContribClassName { get; set; } = string.Empty;

        private ModuleHealthCheckEnum _healthCheck = ModuleHealthCheckEnum.NotTested;

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

        private string? _diag;

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