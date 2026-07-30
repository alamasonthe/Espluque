using Espluque.Contracts.ModuleInterfaces;
using Espluquer.Services;
using System.Windows;
using System.Windows.Controls;

namespace Espluquer.UserControls.Modules
{
    public partial class ModuleTestDetailUC : UserControl
    {
        public static readonly DependencyProperty ModuleDiagnosticProperty =
            DependencyProperty.Register(
                nameof(ModuleDiagnostic),
                typeof(IModuleDiagnostic),
                typeof(ModuleTestDetailUC));

        public IModuleDiagnostic? ModuleDiagnostic
        {
            get => (IModuleDiagnostic?)GetValue(ModuleDiagnosticProperty);
            set => SetValue(ModuleDiagnosticProperty, value);
        }

        public ModuleTestDetailUC()
        {
            InitializeComponent();
        }

        private void ContributionHeader_Loaded( object sender, RoutedEventArgs e)
        {
            if ((sender is not StackPanel header)
                || (header.DataContext is not IModuleContributionDiagnostic contribution))
            {
                return;
            }

            header.Children.Clear();

            TextBlock icon = new()
            {
                Text = ModuleTestService.GetContributionIcon( contribution.InterfaceType)
            };

            icon.SetResourceReference( FrameworkElement.StyleProperty, "ModuleContributionIconStyle");

            string colorKey = ModuleTestService.GetContributionColorKey( contribution.InterfaceType, contribution.ContributionHealthCheck);

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
    }
}