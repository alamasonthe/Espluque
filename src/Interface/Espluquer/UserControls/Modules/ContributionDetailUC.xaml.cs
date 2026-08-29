using Espluque.Contracts.Contributions;
using Espluque.Contracts.Modules;
using Espluquer.Entities;
using Espluquer.Services;
using Espluquer.UserControls.Shell;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace Espluquer.UserControls.Modules
{
    public partial class ContributionDetailUC : UserControl
    {
        public event EventHandler? SettingsChanged;

        public static readonly DependencyProperty ModuleInfoProperty =
            DependencyProperty.Register(
                nameof(ModuleInfo),
                typeof(IModuleInfo),
                typeof(ContributionDetailUC));

        public IModuleInfo? ModuleInfo
        {
            get => (IModuleInfo?)GetValue(ModuleInfoProperty);
            set => SetValue(ModuleInfoProperty, value);
        }


        public static readonly DependencyProperty ContributionHealthsProperty =
            DependencyProperty.Register(
                nameof(ContributionHealths),
                typeof(List<ContributionHealthDto>),
                typeof(ContributionDetailUC));

        public List<ContributionHealthDto> ContributionHealths
        {
            get => (List<ContributionHealthDto>)GetValue(ContributionHealthsProperty);
            set => SetValue(ContributionHealthsProperty, value);
        }


        public static readonly DependencyProperty ConceptSearchProperty =
            DependencyProperty.Register(
                nameof(ConceptSearch),
                typeof(ConceptSearchUC),
                typeof(ContributionDetailUC));

        public ConceptSearchUC? ConceptSearch
        {
            get => (ConceptSearchUC?)GetValue(ConceptSearchProperty);
            set => SetValue(ConceptSearchProperty, value);
        }


        public ContributionDetailUC()
        {
            InitializeComponent();
        }


        private void ContributionHeader_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not Grid
                || DataContext is not IModuleContributionInfo contribution
                || ModuleInfo is null)
            {
                return;
            }

            ContributionHealthDto? contributionHealth =
                ContributionHealths?.FirstOrDefault(health =>
                    health.ModuleName == ModuleInfo.Name
                    && health.ContribInterfaceType == contribution.InterfaceType
                    && health.ContribClassName == contribution.ClassName);

            ContributionHeaderInfo.Children.Clear();

            TextBlock icon = new()
            {
                Text = ModuleTestService.GetContributionIcon(
                    contribution.InterfaceType)
            };

            icon.SetResourceReference(
                FrameworkElement.StyleProperty,
                "ModuleContributionIconStyle");

            icon.DataContext = contributionHealth;

            TextBlock label = new()
            {
                Text = contribution.Label,
                Margin = new Thickness(8, 0, 0, 0)
            };

            label.SetResourceReference(
                FrameworkElement.StyleProperty,
                "App.StandardSubtitleTextBlock");

            label.SetResourceReference(
                TextBlock.ForegroundProperty,
                "App.TextInverse");

            ContributionHeaderInfo.Children.Add(icon);
            ContributionHeaderInfo.Children.Add(label);
        }


        private void ContributionHealth_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox textBox
                || DataContext is not IModuleContributionInfo contribution
                || ModuleInfo is null)
            {
                return;
            }

            ContributionHealthDto? contributionHealth =
                ContributionHealths?.FirstOrDefault(health =>
                    health.ModuleName == ModuleInfo.Name
                    && health.ContribInterfaceType == contribution.InterfaceType
                    && health.ContribClassName == contribution.ClassName);

            if (contributionHealth is not null)
            {
                textBox.SetBinding(
                    TextBox.TextProperty,
                    new Binding(nameof(ContributionHealthDto.HealthCheck))
                    {
                        Source = contributionHealth,
                        Mode = BindingMode.OneWay
                    });
            }
        }


        private void ContributionError_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox textBox
                || DataContext is not IModuleContributionInfo contribution
                || ModuleInfo is null)
            {
                return;
            }

            ContributionHealthDto? contributionHealth =
                ContributionHealths?.FirstOrDefault(health =>
                    health.ModuleName == ModuleInfo.Name
                    && health.ContribInterfaceType == contribution.InterfaceType
                    && health.ContribClassName == contribution.ClassName);

            if (contributionHealth is not null)
            {
                textBox.SetBinding(
                    TextBox.TextProperty,
                    new Binding(nameof(ContributionHealthDto.Diag))
                    {
                        Source = contributionHealth,
                        Mode = BindingMode.OneWay
                    });
            }
        }


        private void AddTagButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConceptSearch is null)
            {
                return;
            }

            // The shared ConceptSearchUC may currently belong
            // to another ContributionDetailUC.
            if (ConceptSearch.Parent is ContentControl previousHost
                && !ReferenceEquals(previousHost, ConceptSearchHost))
            {
                ContributionDetailUC? previousContribution =
                    FindParentContribution(previousHost);

                previousContribution?.CloseConceptSearch();
            }

            ConceptSearch.ConceptSelected -= ConceptSearch_ConceptSelected;
            ConceptSearch.ConceptSelected += ConceptSearch_ConceptSelected;

            ConceptSearch.Clear();

            ConceptSearchHost.Content = ConceptSearch;
            ConceptSearchHost.Visibility = Visibility.Visible;

            AddTagButton.Visibility = Visibility.Collapsed;
        }


        private void RemoveTagButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button
                || button.DataContext is not string tag
                || DataContext is not IModuleContributionInfo contribution)
            {
                return;
            }

            if (contribution.ContributionSettings.Tags.Remove(tag))
            {
                CollectionViewSource
                    .GetDefaultView(contribution.ContributionSettings.Tags)
                    .Refresh();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }


        private void ConceptSearch_ConceptSelected(ConceptDto concept)
        {
            if (DataContext is not IModuleContributionInfo contribution)
            {
                return;
            }

            List<string> tags = contribution.ContributionSettings.Tags;

            if (!tags.Any(tag =>
                string.Equals(
                    tag,
                    concept.Term,
                    StringComparison.OrdinalIgnoreCase)))
            {
                tags.Add(concept.Term);

                CollectionViewSource
                    .GetDefaultView(tags)
                    .Refresh();

                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }

            CloseConceptSearch();
        }


        private void CloseConceptSearch()
        {
            if (ConceptSearch is not null)
            {
                ConceptSearch.ConceptSelected -= ConceptSearch_ConceptSelected;
            }

            ConceptSearchHost.Content = null;
            ConceptSearchHost.Visibility = Visibility.Collapsed;

            AddTagButton.Visibility = Visibility.Visible;
        }


        private static ContributionDetailUC? FindParentContribution(
            DependencyObject element)
        {
            DependencyObject? current = element;

            while (current is not null)
            {
                if (current is ContributionDetailUC contributionDetail)
                {
                    return contributionDetail;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private void ActiveToggle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggleButton)
            {
                toggleButton
                    .GetBindingExpression(ToggleButton.IsCheckedProperty)?
                    .UpdateSource();
            }

            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}