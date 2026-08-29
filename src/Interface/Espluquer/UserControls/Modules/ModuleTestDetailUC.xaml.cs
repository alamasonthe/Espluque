using Espluque.Contracts.Contributions;
using Espluque.Contracts.Modules;
using Espluquer.Entities;
using Espluquer.Services;
using Espluquer.UserControls.Shell;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace Espluquer.UserControls.Modules
{
    public partial class ModuleTestDetailUC : UserControl
    {
        private readonly IModuleService _moduleService;
        private readonly IContributionSettingsService _contributionSettingsService;

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

        private readonly ConceptSearchUC _conceptSearchUC;

        private IModuleContributionInfo? _tagContribution;
        private ContentControl? _tagSearchHost;

        public ModuleTestDetailUC(
            IModuleService moduleService,
            IContributionSettingsService contributionSettingsService,
            ConceptSearchUC conceptSearchUC)
        {
            _moduleService = moduleService;
            _contributionSettingsService = contributionSettingsService;
            _conceptSearchUC = conceptSearchUC;

            InitializeComponent();

            _conceptSearchUC.ConceptSelected += ConceptSearch_ConceptSelected;
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

            ToggleButton activeToggle = new()
            {
                Content = "\uF60E"
            };

            activeToggle.SetResourceReference( FrameworkElement.StyleProperty, "App.ActiveToggleButton");

            activeToggle.SetBinding(
                ToggleButton.IsCheckedProperty,
                new Binding("ContributionSettings.Active")
                {
                    Mode = BindingMode.TwoWay
                });

            TextBlock icon = new()
            {
                Text = ModuleTestService.GetContributionIcon( contribution.InterfaceType)
            };

            icon.SetResourceReference( FrameworkElement.StyleProperty, "ModuleContributionIconStyle");

            /*
            if (contributionHealth is not null)
            {
                icon.SetBinding(TextBlock.ForegroundProperty,
                    new Binding(nameof(ContributionHealthDto.HealthBrush)) { Source = contributionHealth, Mode = BindingMode.OneWay });
            }
            */

            icon.DataContext = contributionHealth;

            TextBlock label = new()
            {
                Text = contribution.Label,
                Margin = new Thickness(8, 0, 0, 0)
            };

            label.SetResourceReference( FrameworkElement.StyleProperty, "App.StandardSubtitleTextBlock");
            label.SetResourceReference( TextBlock.ForegroundProperty, "App.TextInverse");

            header.Children.Add(activeToggle);
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

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ModuleInfo is null)
            {
                return;
            }

            await _moduleService.SaveModuleInfo(ModuleInfo);
        }

        private async void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            if (ModuleInfo is null)
            {
                return;
            }

            SaveFileDialog dialog = new()
            {
                FileName = Path.GetFileName(ModuleInfo.FilePath),
                InitialDirectory = Path.GetDirectoryName(ModuleInfo.FilePath),
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = ".json"
            };

            if (dialog.ShowDialog() == true)
            {
                await _moduleService.SaveModuleInfo(ModuleInfo, dialog.FileName);
            }
        }

        private async void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (ModuleInfo is null)
            {
                return;
            }

            foreach (IModuleContributionInfo contribution in ModuleInfo.Contributions)
            {
                await _contributionSettingsService.SaveUserSettings(
                    ModuleInfo.Assembly,
                    contribution.InterfaceType,
                    contribution.ClassName,
                    contribution.ContributionSettings);
            }
        }

        private void ExploreButton_Click(object sender, RoutedEventArgs e)
        {
            if (ModuleInfo is null)
            {
                return;
            }

            string? directory = Path.GetDirectoryName(ModuleInfo.FilePath);

            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{directory}\"",
                UseShellExecute = true
            });
        }

        private void AddTagButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button
                || button.DataContext is not IModuleContributionInfo contribution
                || button.Parent is not StackPanel tagsPanel)
            {
                return;
            }

            ContentControl? searchHost =
                tagsPanel.Children
                    .OfType<ContentControl>()
                    .FirstOrDefault(control => control.Name == "ConceptSearchHost");

            if (searchHost is null)
            {
                return;
            }

            if (_tagSearchHost is not null)
            {
                Button? previousButton = GetAddTagButton(_tagSearchHost);

                if (previousButton is not null)
                {
                    previousButton.Visibility = Visibility.Visible;
                }

                _tagSearchHost.Content = null;
                _tagSearchHost.Visibility = Visibility.Collapsed;
            }

            _tagContribution = contribution;
            _tagSearchHost = searchHost;

            _conceptSearchUC.Clear();

            searchHost.Content = _conceptSearchUC;
            searchHost.Visibility = Visibility.Visible;
            button.Visibility = Visibility.Collapsed;
            searchHost.UpdateLayout();

        }

        private void RemoveTagButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button
                || button.DataContext is not string tag
                || button.Tag is not IModuleContributionInfo contribution)
            {
                return;
            }

            if (contribution.ContributionSettings.Tags.Remove(tag))
            {
                CollectionViewSource
                    .GetDefaultView(contribution.ContributionSettings.Tags)
                    .Refresh();
            }
        }

        private void ConceptSearch_ConceptSelected(ConceptDto concept)
        {
            if (_tagContribution is null || _tagSearchHost is null)
            {
                return;
            }

            List<string> tags = _tagContribution.ContributionSettings.Tags;

            if (!tags.Any(tag =>
                string.Equals(tag, concept.Term, StringComparison.OrdinalIgnoreCase)))
            {
                tags.Add(concept.Term);

                CollectionViewSource
                    .GetDefaultView(tags)
                    .Refresh();
            }

            Button? addButton = GetAddTagButton(_tagSearchHost);

            if (addButton is not null)
            {
                addButton.Visibility = Visibility.Visible;
            }

            _tagSearchHost.Content = null;
            _tagSearchHost.Visibility = Visibility.Collapsed;

            _tagSearchHost.Visibility = Visibility.Collapsed;

            _tagSearchHost = null;
            _tagContribution = null;
        }

        private static Button? GetAddTagButton(ContentControl searchHost)
        {
            if (searchHost.Parent is not StackPanel tagsPanel)
            {
                return null;
            }

            return tagsPanel.Children
                .OfType<Button>()
                .FirstOrDefault(button => button.Name == "AddTagButton");
        }
    }
}