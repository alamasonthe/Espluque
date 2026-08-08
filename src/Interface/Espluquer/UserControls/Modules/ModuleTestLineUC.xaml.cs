using Espluque.Contracts.Enums;
using Espluque.Contracts.ModuleInterfaces;
using Espluquer.Services;
using System.Windows;
using System.Windows.Controls;

namespace Espluquer.UserControls.Modules
{
    public partial class ModuleTestLineUC : UserControl
    {
        private static int _styleTestIndex;
        private IModuleInfo? ModuleInfo => DataContext as IModuleInfo;
        public static readonly DependencyProperty ContributionHealthsProperty =
            DependencyProperty.Register(
                nameof(ContributionHealths),
                typeof(List<IContributionHealth>),
                typeof(ModuleTestLineUC));

        public List<IContributionHealth> ContributionHealths
        {
            get => (List<IContributionHealth>)GetValue(ContributionHealthsProperty);
            set => SetValue(ContributionHealthsProperty, value);
        }

        private ModuleHealthCheckEnum ModuleHealthCheck
        {
            get
            {
                if (ModuleInfo is null)
                {
                    return ModuleHealthCheckEnum.NotTested;
                }

                var moduleHealths = ContributionHealths
                    .Where(health => health.ModuleName == ModuleInfo.Name)
                    .ToList();

                if (moduleHealths.Count == 0)
                {
                    return ModuleHealthCheckEnum.NotTested;
                }

                if (moduleHealths.Any(health => health.HealthCheck == ModuleHealthCheckEnum.Error))
                {
                    return ModuleHealthCheckEnum.Error;
                }

                if (moduleHealths.Any(health => health.HealthCheck == ModuleHealthCheckEnum.Running))
                {
                    return ModuleHealthCheckEnum.Running;
                }

                if (moduleHealths.All(health => health.HealthCheck == ModuleHealthCheckEnum.Success))
                {
                    return ModuleHealthCheckEnum.Success;
                }

                return ModuleHealthCheckEnum.NotTested;
            }
        }

        public ModuleTestLineUC()
        {
            InitializeComponent();

            Loaded += (_, _) =>
            {
                ContributionSummaryHost.Content = CreateContributionSummary();
                UpdateModuleHealth();
            };
        }

        #region Build icon panel

        private StackPanel CreateContributionSummary()
        {
            StackPanel summaryPanel = new()
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (ModuleInfo is null)
            {
                return summaryPanel;
            }

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
                    TextBlock separator = new()
                    {
                        Text = "  ",
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    separator.SetResourceReference(
                        TextBlock.ForegroundProperty,
                        "App.TextMuted");

                    summaryPanel.Children.Add(separator);
                }

                int totalCount = contributionGroup.Count();

                int successCount = contributionGroup.Count(
                    health => health.HealthCheck == ModuleHealthCheckEnum.Success);

                bool groupHasError = contributionGroup.Any(
                    health => health.HealthCheck == ModuleHealthCheckEnum.Error);

                ModuleHealthCheckEnum groupHealthCheck = groupHasError switch
                {
                    true => ModuleHealthCheckEnum.Error,
                    false when successCount == totalCount => ModuleHealthCheckEnum.Success,
                    _ => ModuleHealthCheckEnum.NotTested
                };

                TextBlock icon = new()
                {
                    Text = ModuleTestService.GetContributionIcon(contributionGroup.Key),
                    FontSize = 20,
                    VerticalAlignment = VerticalAlignment.Center
                };

                icon.SetResourceReference(
                    TextBlock.FontFamilyProperty,
                    "FluentIcons");

                string colorKey = ModuleTestService.GetContributionColorKey(
                    contributionGroup.Key,
                    groupHealthCheck);

                icon.SetResourceReference(
                    TextBlock.ForegroundProperty,
                    colorKey);

                TextBlock count = new()
                {
                    Text = $" ({successCount}/{totalCount})",
                    VerticalAlignment = VerticalAlignment.Center
                };

                count.SetResourceReference(
                    TextBlock.ForegroundProperty,
                    "App.Text");

                summaryPanel.Children.Add(icon);
                summaryPanel.Children.Add(count);
            }

            return summaryPanel;
        }

        #endregion

        private void UpdateModuleHealth()
        {
            string colorKey = ModuleTestService.GetHealthColorKey(ModuleHealthCheck);

            StatusEllipse.SetResourceReference(
                System.Windows.Shapes.Shape.FillProperty,
                colorKey);
        }
    }
}
