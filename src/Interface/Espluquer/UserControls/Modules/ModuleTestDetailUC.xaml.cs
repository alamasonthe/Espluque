using Espluque.Contracts.Enums;
using Espluque.Contracts.ModuleInterfaces;
using Espluquer.Services;
using System.Windows;
using System.IO;
using System.Windows.Controls;

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

        public List<IContributionHealth> ContributionHealths { get; set; } = [];

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

            IContributionHealth? contributionHealth =
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

            string colorKey = ModuleTestService.GetContributionColorKey(contribution.InterfaceType, contributionHealth?.HealthCheck ?? ModuleHealthCheckEnum.NotTested);
            icon.SetResourceReference( TextBlock.ForegroundProperty, colorKey);

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

            IContributionHealth? contributionHealth =
                ContributionHealths.FirstOrDefault(health =>
                    health.ModuleName == ModuleInfo.Name &&
                    health.ContribInterfaceType == contribution.InterfaceType &&
                    health.ContribClassName == contribution.ClassName);

            textBox.Text =
                (contributionHealth?.HealthCheck ?? ModuleHealthCheckEnum.NotTested).ToString();
        }

        private void ContributionError_Loaded(object sender, RoutedEventArgs e)
        {
            if ((sender is not TextBox textBox)
                || (textBox.DataContext is not IModuleContributionInfo contribution)
                || ModuleInfo is null)
            {
                return;
            }

            IContributionHealth? contributionHealth =
                ContributionHealths.FirstOrDefault(health =>
                    health.ModuleName == ModuleInfo.Name &&
                    health.ContribInterfaceType == contribution.InterfaceType &&
                    health.ContribClassName == contribution.ClassName);

            textBox.Text = contributionHealth?.ErrorDescription ?? string.Empty;
        }
    }
}