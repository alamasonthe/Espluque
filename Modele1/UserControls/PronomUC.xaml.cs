using Espluque.Application.Services;
using Microsoft.Win32;
using PronomSqlite;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Modele1.UserControls
{

    public partial class PronomUC : UserControl
    {
        private readonly PronomService _pronomService;

        public PronomUC(PronomService pronomService)
        {
            InitializeComponent();

            _pronomService = pronomService;
        }

        private void SelectFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
            {
                return;
            }

            OpenFileDialog openFileDialog = new();
            openFileDialog.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";

            bool? isSelected = openFileDialog.ShowDialog();

            if (isSelected != true)
            {
                return;
            }

            switch (button.Tag?.ToString())
            {
                case "File":
                    FileTextBox.Text = openFileDialog.FileName;
                    break;

                case "Container":
                    ContainerTextBox.Text = openFileDialog.FileName;
                    break;
            }
        }

        private async void Import_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(FileTextBox.Text))
            {
                await _pronomService.ImportFileExtensionFromXmlAsync(FileTextBox.Text);
            }
            /*
            if (!string.IsNullOrWhiteSpace(ContainerTextBox.Text))
            {
                await _PronomService.ImportContainerExtensionFromXmlAsync(ContainerTextBox.Text);
            }
            */
        }
    }
}
