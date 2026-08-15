using Espluquer.Services;
using System.Windows;
using System.Windows.Controls;
using Espluquer.Entities;
using Espluque.Contracts.Modules;

namespace Espluquer.UserControls.Modules
{
    public partial class ModuleTestLineUC : UserControl
    {
        private static int _styleTestIndex;
        private IModuleInfo? ModuleInfo => DataContext as IModuleInfo;
        public static readonly DependencyProperty ContributionHealthsProperty = DependencyProperty.Register(
                nameof(ContributionHealths),
                typeof(List<ContributionHealthDto>),
                typeof(ModuleTestLineUC));

        public List<ContributionHealthDto> ContributionHealths
        {
            get => (List<ContributionHealthDto>)GetValue(ContributionHealthsProperty);
            set => SetValue(ContributionHealthsProperty, value);
        }

        public static readonly DependencyProperty ModuleHealthProperty =
            DependencyProperty.Register(
                nameof(ModuleHealth),
                typeof(ModuleHealthDto),
                typeof(ModuleTestLineUC));

        public ModuleHealthDto ModuleHealth
        {
            get => (ModuleHealthDto)GetValue(ModuleHealthProperty);
            set => SetValue(ModuleHealthProperty, value);
        }

        public ModuleTestLineUC()
        {
            InitializeComponent();

            Loaded += (_, _) =>
            {
                ContributionSummaryHost.Content = CreateContributionSummary();
            };
        }

        private StackPanel CreateContributionSummary()
        {
            StackPanel summaryPanel = new()
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (ModuleInfo is null)
                return summaryPanel;

            var contributionGroups = ContributionHealths
                .Where(health => health.ModuleName == ModuleInfo.Name)
                .GroupBy(health => health.ContribInterfaceType)
                .OrderBy(group => ModuleTestService.GetContributionDisplayOrder(group.Key))
                .ThenBy(group => group.Key)
                .ToList();

            for (int contribGroupIndex = 0; contribGroupIndex < contributionGroups.Count; contribGroupIndex++)
            {
                var contributionGroup = contributionGroups[contribGroupIndex];

                if (contribGroupIndex > 0)
                {
                    TextBlock separator = new() { Text = "  ", VerticalAlignment = VerticalAlignment.Center };
                    separator.SetResourceReference(TextBlock.ForegroundProperty, "App.TextMuted");
                    summaryPanel.Children.Add(separator);
                }

                summaryPanel.Children.Add(
                    new ContributionHealthGroup(
                        contributionGroup.Key,
                        contributionGroup.ToList())
                    .DisplayBlock);
            }

            return summaryPanel;
        }
    }
}
