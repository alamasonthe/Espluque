using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using Espluque.Application.Services;

namespace Modele1.UserControls
{
    public partial class DyneUC : UserControl
    {
        private readonly DyneService _dyneService;

        public DyneUC(DyneService dyneService)
        {
            InitializeComponent();

            _dyneService = dyneService;
        }

        private void SelectFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
            {
                return;
            }

            OpenFileDialog openFileDialog = new();

            switch (button.Tag?.ToString())
            {
                case "csv":
                    openFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                    break;

                case "json":
                    openFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                    break;

                default:
                    return;
            }

            bool? isSelected = openFileDialog.ShowDialog();

            if (isSelected != true)
            {
                return;
            }

            switch (button.Tag?.ToString())
            {
                case "csv":
                    ExtensionsCsvTextBox.Text = openFileDialog.FileName;
                    break;

                case "json":
                    ExtensionsJsonTextBox.Text = openFileDialog.FileName;
                    break;
            }
        }

        private async void Import_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(ExtensionsCsvTextBox.Text))
            {
                await _dyneService.ImportExtensionFromCsv(ExtensionsCsvTextBox.Text);
            }
            if (!string.IsNullOrWhiteSpace(ExtensionsJsonTextBox.Text))
            {
                await _dyneService.ImportExtensionCategoryFromJson(ExtensionsJsonTextBox.Text);
            }
        }
    }
}