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

        public static readonly DependencyProperty ModuleDiagnosticProperty =
            DependencyProperty.Register(
                nameof(ModuleDiagnostic),
                typeof(IModuleDiagnostic),
                typeof(ModuleTestLineUC),
                new PropertyMetadata(null, ModuleDiagnosticChanged));

        public IModuleDiagnostic? ModuleDiagnostic
        {
            get => (IModuleDiagnostic?)GetValue(ModuleDiagnosticProperty);
            set => SetValue(ModuleDiagnosticProperty, value);
        }

        public ModuleTestLineUC()
        {
            InitializeComponent();

            // Loaded += ModuleTestLineUC_Loaded;
        }

        #region Build icon panel

        private StackPanel CreateContributionSummary()
        {
            StackPanel summaryPanel = new()
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (ModuleDiagnostic is null)
            {
                return summaryPanel;
            }

            var contributionGroups = ModuleDiagnostic.Contributions.GroupBy(contribution => contribution.InterfaceType).ToList();

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
                    separator.SetResourceReference(TextBlock.ForegroundProperty,"App.TextMuted");
                    summaryPanel.Children.Add(separator);
                }

                int totalCount = contributionGroup.Count();
                int successCount = contributionGroup.Count( contribution => contribution.ContributionHealthCheck == ModuleHealthCheckEnum.Success);

                bool groupHasError = false;
                ModuleHealthCheckEnum groupHealthCheck = groupHasError switch
                {
                    true => ModuleHealthCheckEnum.Error,
                    false when successCount == totalCount => ModuleHealthCheckEnum.Success,
                    _ => ModuleHealthCheckEnum.NotTested
                };

                TextBlock icon = new()
                {
                    Text = ModuleTestService.GetContributionIcon(contributionGroup.Key), FontSize = 20, VerticalAlignment = VerticalAlignment.Center
                };

                icon.SetResourceReference( TextBlock.FontFamilyProperty, "FluentIcons");

                string colorKey = ModuleTestService.GetContributionColorKey(contributionGroup.Key, groupHealthCheck);

                icon.SetResourceReference( TextBlock.ForegroundProperty, colorKey);

                TextBlock count = new()
                {
                    Text = $" ({successCount}/{totalCount})",
                    VerticalAlignment = VerticalAlignment.Center
                };

                count.SetResourceReference( TextBlock.ForegroundProperty, "App.Text");

                summaryPanel.Children.Add(icon);
                summaryPanel.Children.Add(count);
            }

            return summaryPanel;
        }

        #endregion

        #region Module diagnostic change management

        private static void ModuleDiagnosticChanged( DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            ModuleTestLineUC moduleTestLine = (ModuleTestLineUC)dependencyObject;

            moduleTestLine.UpdateContributionSummary();
        }

        private void UpdateContributionSummary()
        {
            ContributionSummaryHost.Content = null;

            if ((ModuleDiagnostic is null)
                || (ModuleDiagnostic.ModuleHealthCheck == ModuleHealthCheckEnum.NotTested)
                || (ModuleDiagnostic.Contributions.Count == 0))
            {
                return;
            }

            ContributionSummaryHost.Content = CreateContributionSummary();
        }

        #endregion
    }
}
