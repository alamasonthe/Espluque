using Espluque.Contracts.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Pronom.UserControls
{
    public partial class MaintenanceUC : UserControl
    {
        private readonly Espluque.Contracts.Ports.ILogger _logger;
        private readonly ISettingsService _settingsService;

        public MaintenanceUC(Espluque.Contracts.Ports.ILogger logger, ISettingsService settingsService)
        {
            _logger = logger;
            _settingsService = settingsService;

            InitializeComponent();
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
                await ImportFileExtensionFromXmlAsync(FileTextBox.Text);
            }
            if (!string.IsNullOrWhiteSpace(ContainerTextBox.Text))
            {
                await ImportContainerSignatureFromXmlAsync(ContainerTextBox.Text);
            }
        }

        private static string GetDbFilePath(ISettingsService settingsService)
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appName = Assembly.GetEntryAssembly()?.GetName().Name ?? Process.GetCurrentProcess().ProcessName;
            string appDirectoryPath = Path.Combine(appDataPath, appName);

            string? settingsDbFileName = settingsService
                .GetSetting("PronomDb")
                .GetAwaiter()
                .GetResult();

            if (string.IsNullOrWhiteSpace(settingsDbFileName))
            {
                settingsDbFileName = "pronom.db";
            }

            string dbFilePath;

            if (Path.IsPathRooted(settingsDbFileName))
            {
                dbFilePath = settingsDbFileName;
            }
            else
            {
                dbFilePath = Path.Combine(appDirectoryPath, settingsDbFileName);
            }

            string? directoryPath = Path.GetDirectoryName(dbFilePath);

            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            return dbFilePath;
        }

        public async Task<bool> ImportFileExtensionFromXmlAsync(string filePath)
        {
            var readXmlResult = await Util.Xml.ReadXDocumentFromFile(filePath);
            if (!readXmlResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"File signature Xml check failed: {readXmlResult.Error?.Code} - {readXmlResult.Error?.Message}");
                return false;
            }
            _logger.Log(LogLevel.Information, $"File signature Xml check succeeded: {filePath}");

            var importResult = await FileSignatureXmlReader.ImportSignaturesFromXmlAsync(readXmlResult.Value!, GetDbFilePath(_settingsService));
            if (!importResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"File signature Xml import failed: {importResult.Error?.Code} - {importResult.Error?.Message}");
                return false;
            }
            _logger.Log(LogLevel.Information, $"File signature Xml import succeeded: {filePath}");

            return true;
        }

        public async Task<bool> ImportContainerSignatureFromXmlAsync(string filePath)
        {
            var readXmlResult = await Util.Xml.ReadXDocumentFromFile(filePath);
            if (!readXmlResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"Container signature Xml check failed: {readXmlResult.Error?.Code} - {readXmlResult.Error?.Message}");
                return false;
            }
            _logger.Log(LogLevel.Information, $"Container signature Xml check succeeded: {filePath}");

            var importResult = await ContainerSignatureXmlReader.ImportSignaturesFromXmlAsync(readXmlResult.Value!, GetDbFilePath(_settingsService));
            if (!importResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"Container signature Xml import failed: {importResult.Error?.Code} - {importResult.Error?.Message}");
                return false;
            }
            _logger.Log(LogLevel.Information, $"Container signature Xml import succeeded: {filePath}");

            return true;
        }

    }
}
