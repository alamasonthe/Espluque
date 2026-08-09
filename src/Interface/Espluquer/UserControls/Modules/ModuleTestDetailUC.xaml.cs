using Espluque.Contracts.ModuleInterfaces;
using Espluquer.Entities;
using Espluquer.Services;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Espluquer.UserControls.Modules
{
    public partial class ModuleTestDetailUC : UserControl
    {
        public IModuleInfo? ModuleInfo
        {
            get => DataContext as IModuleInfo;
            set
            {
                DataContext = value;

                JsonTextBox.Text =
                    value is not null && File.Exists(value.FilePath)
                        ? File.ReadAllText(value.FilePath)
                        : string.Empty;
            }
        }

        public List<ContributionHealthDto> ContributionHealths { get; set; } = [];

        public static readonly DependencyProperty ModuleHealthProperty = DependencyProperty.Register(nameof(ModuleHealth), typeof(ModuleHealthDto), typeof(ModuleTestDetailUC));

        public ModuleHealthDto? ModuleHealth
        {
            get => (ModuleHealthDto?)GetValue(ModuleHealthProperty);
            set => SetValue(ModuleHealthProperty, value);
        }

        public ModuleTestDetailUC()
        {
            InitializeComponent();
        }

        private void ContributionHeader_Loaded( object sender, RoutedEventArgs e)
        {
            if ((sender is not StackPanel header)
                || (header.DataContext is not IModuleContributionInfo contribution)
                || ModuleInfo is null)
            {
                return;
            }

            ContributionHealthDto? contributionHealth =
                ContributionHealths.FirstOrDefault(health =>
                    health.ModuleName == ModuleInfo.Name &&
                    health.ContribInterfaceType == contribution.InterfaceType &&
                    health.ContribClassName == contribution.ClassName);

            header.Children.Clear();

            TextBlock icon = new()
            {
                Text = ModuleTestService.GetContributionIcon( contribution.InterfaceType)
            };

            icon.SetResourceReference( FrameworkElement.StyleProperty, "ModuleContributionIconStyle");

            if (contributionHealth is not null)
            {
                icon.SetBinding(TextBlock.ForegroundProperty,
                    new Binding(nameof(ContributionHealthDto.HealthBrush)) { Source = contributionHealth, Mode = BindingMode.OneWay });
            }

            TextBlock label = new()
            {
                Text = contribution.Label,
                Margin = new Thickness(8, 0, 0, 0)
            };

            label.SetResourceReference( FrameworkElement.StyleProperty, "App.StandardSubtitleTextBlock");
            label.SetResourceReference( TextBlock.ForegroundProperty, "App.TextInverse");

            header.Children.Add(icon);
            header.Children.Add(label);
        }

        private void ContributionHealth_Loaded(object sender, RoutedEventArgs e)
        {
            if ((sender is not TextBox textBox)
                || (textBox.DataContext is not IModuleContributionInfo contribution)
                || ModuleInfo is null)
            {
                return;
            }

            ContributionHealthDto? contributionHealth =
                ContributionHealths.FirstOrDefault(health =>
                    health.ModuleName == ModuleInfo.Name &&
                    health.ContribInterfaceType == contribution.InterfaceType &&
                    health.ContribClassName == contribution.ClassName);

            if (contributionHealth is not null)
            {
                textBox.SetBinding(TextBox.TextProperty,
                    new Binding(nameof(ContributionHealthDto.HealthCheck)) { Source = contributionHealth, Mode = BindingMode.OneWay });
            }
        }

        private void ContributionError_Loaded(object sender, RoutedEventArgs e)
        {
            if ((sender is not TextBox textBox)
                || (textBox.DataContext is not IModuleContributionInfo contribution)
                || ModuleInfo is null)
            {
                return;
            }

            ContributionHealthDto? contributionHealth =
                ContributionHealths.FirstOrDefault(health =>
                    health.ModuleName == ModuleInfo.Name &&
                    health.ContribInterfaceType == contribution.InterfaceType &&
                    health.ContribClassName == contribution.ClassName);

            if (contributionHealth is not null)
            {
                textBox.SetBinding(TextBox.TextProperty,
                    new Binding(nameof(ContributionHealthDto.Diag)) { Source = contributionHealth, Mode = BindingMode.OneWay });
            }
        }
    }
}