using Espluque.Contracts.CrossCutting;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Util;
using WindowsInstaller.Files;
using System.IO;

namespace WindowsInstaller
{
    public partial class MsiFileViewerUC : UserControl
    {
        private readonly string _filename;
        private readonly string _tempFolderPath;
        private readonly WindowsInstallerService _windowsInstallerService;
        private readonly FileService _fileService;
        private readonly IMessageCenter _messageCenter;
        private readonly IEntityFactory _entityFactory;
        private readonly ILogger _logger;


        private TreeNode<MsiDirectoryItem>? _tree;

        public MsiFileViewerUC( string filename, string tempFolderPath, IMessageCenter messageCenter, IEntityFactory entityFactory, ILogger logger)
        {
            InitializeComponent();

            _filename = filename;
            _tempFolderPath = tempFolderPath;
            _messageCenter = messageCenter;
            _entityFactory = entityFactory;
            _logger = logger;

            _windowsInstallerService = new WindowsInstallerService();
            _fileService = new FileService(_windowsInstallerService);

            LoadTree();
        }

        private void LoadTree()
        {
            _tree = _fileService.GetTree(_filename);

            if (_tree == null)
                return;

            FileTreeView.ItemsSource = _tree.BranchChildren;

            FileTreeView.UpdateLayout();

            TreeViewItem? installDirItem = FindDirectoryItem( FileTreeView, "INSTALLDIR");

            if (installDirItem != null)
            {
                installDirItem.IsSelected = true;
                installDirItem.BringIntoView();
            }
        }

        private static TreeViewItem? FindDirectoryItem( ItemsControl parent, string directoryKey)
        {
            foreach (object item in parent.Items)
            {
                TreeViewItem? container =
                    parent.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;

                if (container == null)
                    continue;

                if (item is TreeNode<MsiDirectoryItem> node &&
                    node.Data?.DirectoryKey == directoryKey)
                {
                    return container;
                }

                container.IsExpanded = true;
                container.UpdateLayout();

                TreeViewItem? result = FindDirectoryItem(
                    container,
                    directoryKey);

                if (result != null)
                    return result;
            }

            return null;
        }

        private void FileTreeView_SelectedItemChanged( object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeNode<MsiDirectoryItem> selectedNode)
            {
                ShowFiles(selectedNode);
            }
        }

        private void ShowFiles(TreeNode<MsiDirectoryItem> node)
        {
            FileDataGrid.ItemsSource = node.Data?.Files;
        }

        private async void FileDataGrid_MouseDoubleClick( object sender, MouseButtonEventArgs e)
        {
            if (FileDataGrid.SelectedItem is not MsiFileItem file)
                return;

            string? cabinet = _fileService.GetCabinet(_filename, file);

            if (cabinet == null)
            {
                string? msiDirectory = Path.GetDirectoryName(_filename);

                if (string.IsNullOrWhiteSpace(msiDirectory))
                    return;

                string externalFilePath = Path.Combine( msiDirectory, file.SourcePath);

                IMessage message = _entityFactory.CreateMessage(
                    MessageTypeEnum.Analyze,
                    "Analyze",
                    [
                        new("FilePath", externalFilePath)
                    ]);

                await _messageCenter.SendAsync(message);
            }

            else if (cabinet.StartsWith('#'))
            {
                string embeddedCabinetName = cabinet[1..];

                string extractedCabinetPath = Path.Combine( _tempFolderPath, embeddedCabinetName);

                var extractCabinetResult =
                    SevenZip.Services.SevenZipService.ExtractEntryToFile(
                        _filename,
                        embeddedCabinetName,
                        extractedCabinetPath);

                if (!extractCabinetResult.IsSuccess)
                {
                    _logger.Log( Microsoft.Extensions.Logging.LogLevel.Error, $"Cannot extract embedded cabinet {embeddedCabinetName} from {_filename}: {extractCabinetResult.Error?.Message}");
                    return;
                }

                IMessage message = _entityFactory.CreateMessage(
                    MessageTypeEnum.ExtractAndAnalyze,
                    "ExtractAndAnalyze",
                    [
                        new("FilePath", extractedCabinetPath),
                        new("InternalPath", file.FileKey)
                    ]);

                await _messageCenter.SendAsync(message);
            }

            else
            {
                string externalCabinetName = cabinet;

                string? msiDirectory = Path.GetDirectoryName(_filename);

                if (string.IsNullOrWhiteSpace(msiDirectory))
                    return;

                string cabinetPath = Path.Combine(
                    msiDirectory,
                    externalCabinetName);

                IMessage message = _entityFactory.CreateMessage(
                    MessageTypeEnum.ExtractAndAnalyze,
                    "ExtractAndAnalyze",
                    [
                        new("FilePath", cabinetPath),
                        new("InternalPath", file.FileKey)
                    ]);

                await _messageCenter.SendAsync(message);
            }

            e.Handled = true;
        }

    }
}