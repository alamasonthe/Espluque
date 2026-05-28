using Espluque.Contracts.Orchestrators;
using Microsoft.Extensions.Logging;
using Modele1.UserControls;
using System.Windows;
using System.Windows.Controls;

namespace Modele1
{
    public partial class MainWindow : Window
    {
        private readonly LogUC _logUC;
        private readonly DyneUC _dyneUC;
        private readonly PronomUC _pronomUC;
        private readonly Espluque.Contracts.Ports.ILogger _logger;

        private readonly IAnalyzer _analyzer;

        public MainWindow(LogUC logUC, DyneUC dyneUC, PronomUC pronomUC, Espluque.Contracts.Ports.ILogger logger, IAnalyzer analyzer)
        {
            InitializeComponent();

            _logUC = logUC;
            _dyneUC = dyneUC;
            _pronomUC = pronomUC;
            _logger = logger;
            _analyzer = analyzer;
            // ContentHost.Content = _logUC;
            InfoHost.Content = _logUC;
        }

        private void MainWindow_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;

            e.Handled = true;
        }

        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] filePaths)
            {
                string firstFilePath = filePaths[0];
                _logger.Log(LogLevel.Information, $"File received: {firstFilePath}");
                _analyzer.AnalyzeFile(firstFilePath);
            }

            e.Handled = true;
        }

        private void Menu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
            {
                return;
            }

            switch (button.Tag?.ToString())
            {
                case "log":
                    ContentHost.Content = _logUC;
                    break;

                case "dyne":
                    ContentHost.Content = _dyneUC;
                    break;

                case "pronom":
                    ContentHost.Content = _pronomUC;
                    break;
            }
        }

    }
}
