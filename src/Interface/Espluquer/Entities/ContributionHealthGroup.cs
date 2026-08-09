using Espluque.Contracts.Enums;
using Espluquer.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace Espluquer.Entities
{
    public class ContributionHealthGroup : INotifyPropertyChanged
    {
        public string InterfaceType { get; }
        public List<ContributionHealthDto> Contributions { get; }
        public int SuccessCount => Contributions.Count(x => x.HealthCheck == ModuleHealthCheckEnum.Success);
        public int TotalCount => Contributions.Count;
        public string CountText => $" ({SuccessCount}/{TotalCount})";
        public Brush HealthBrush => ModuleTestService.GetHealthBrush(HealthCheck);
        public TextBlock DisplayBlock { get; }

        public ModuleHealthCheckEnum HealthCheck
        {
            get
            {
                if (Contributions.Any(x => x.HealthCheck == ModuleHealthCheckEnum.Error))
                    return ModuleHealthCheckEnum.Error;

                if (Contributions.All(x => x.HealthCheck == ModuleHealthCheckEnum.Success))
                    return ModuleHealthCheckEnum.Success;

                return ModuleHealthCheckEnum.NotTested;
            }
        }

        public ContributionHealthGroup(string interfaceType, List<ContributionHealthDto> contributions)
        {
            InterfaceType = interfaceType;
            Contributions = contributions;

            foreach (ContributionHealthDto contribution in Contributions)
                contribution.PropertyChanged += Contribution_PropertyChanged;

            DisplayBlock = CreateDisplayBlock();
        }

        private TextBlock CreateDisplayBlock()
        {
            TextBlock textBlock = new() { VerticalAlignment = VerticalAlignment.Center };

            Run icon = new()
            {
                Text = ModuleTestService.GetContributionIcon(InterfaceType),
                FontSize = 20,
                BaselineAlignment = BaselineAlignment.Center
            };

            icon.SetResourceReference(TextElement.FontFamilyProperty, "FluentIcons");
            icon.SetBinding(TextElement.ForegroundProperty, new Binding(nameof(HealthBrush)) { Source = this });

            Run count = new()
            {
                BaselineAlignment = BaselineAlignment.Center
            };
            count.SetBinding(TextElement.ForegroundProperty, new Binding(nameof(HealthBrush)) { Source = this });
            count.SetBinding(Run.TextProperty, new Binding(nameof(CountText)) { Source = this, Mode = BindingMode.OneWay });

            textBlock.Inlines.Add(icon);
            textBlock.Inlines.Add(count);

            return textBlock;
        }

        private void Contribution_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ContributionHealthDto.HealthCheck))
                return;

            OnPropertyChanged(nameof(SuccessCount));
            OnPropertyChanged(nameof(CountText));
            OnPropertyChanged(nameof(HealthCheck));
            OnPropertyChanged(nameof(HealthBrush));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}