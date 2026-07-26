using Espluque.Contracts.ModuleInterfaces;
using Espluquer.Entities;
using Espluquer.Services;
using System.Windows;
using System.Windows.Controls;

namespace Espluquer.UserControls.Components
{
    public partial class ContributionDetailsUC : UserControl
    {
        private readonly ConceptDto _conceptDto;
        private readonly List<ICatalogEntry> _catalog;

        public ContributionDetailsUC(ConceptDto conceptDto, List<ICatalogEntry> catalog)
        {
            _conceptDto = conceptDto;
            _catalog = catalog;

            InitializeComponent();

            DataContext = _conceptDto;

            ContributionsList.ItemsSource = SelectContribution();
        }

        private List<ICatalogEntry> SelectContribution()
        {
            string? conceptTerm = _conceptDto.Term;

            if (string.IsNullOrWhiteSpace(conceptTerm))
            {
                return [];
            }

            return _catalog
                .Where(entry => entry.Tags.Contains(conceptTerm, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        private void ContributionHeader_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not StackPanel header ||
                header.DataContext is not ICatalogEntry contribution)
            {
                return;
            }

            header.Children.Clear();

            TextBlock icon = new()
            {
                Text = ModuleTestService.GetContributionIcon(contribution.InterfaceType)
            };

            icon.SetResourceReference( FrameworkElement.StyleProperty, "ModuleContributionIconStyle");

            TextBlock label = new()
            {
                Text = contribution.Label, Margin = new Thickness(8, 0, 0, 0)
            };

            label.SetResourceReference( FrameworkElement.StyleProperty, "App.StandardSubtitleTextBlock");

            header.Children.Add(icon);
            header.Children.Add(label);
        }

    }
}
