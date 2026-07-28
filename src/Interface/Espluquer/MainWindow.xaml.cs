using Espluque.Contracts.Entities;
using Espluque.Contracts.Enums;
using Espluque.Contracts.MessageInterfaces;
using Espluque.Contracts.ModuleInterfaces;
using Espluque.Contracts.ModuleInterfaces.Contributions;
using Espluque.Contracts.Orchestrators;
using Espluque.Contracts.Ports;
using Espluque.Theming.Services;
using Espluquer.UserControls.Components;
using Espluquer.UserControls.FileViews;
using Espluquer.UserControls.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Espluquer.UserControls.Parameters;

namespace Espluquer
{
    public partial class MainWindow : Window, IMessageClient
    {
        private string _themeTag = "Light";

        private readonly Espluque.Contracts.Ports.ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IOrchestratorFactory _orchestratorFactory;
        private readonly IMessageCenter _messageCenter;
        private readonly IModuleAdministrationService _moduleAdministrationService;

        private readonly LogUC _logUC;
        private readonly ThesaurusExplorerUC _thesaurusExplorerUC;
        private readonly ModuleDiagnosticUC _moduleDiagnosticUC;
        private readonly ModuleContributionsUC _moduleContributionsUC;
        private readonly List<ICatalogEntry> _catalog;

        private readonly List<string> _recentFiles = [];

        string _startingTag = "AnyFile";

        public MainWindow(LogUC logUC, ThesaurusExplorerUC thesaurusExplorerUC, ModuleDiagnosticUC moduleDiagnosticUC, Espluque.Contracts.Ports.ILogger logger, IOrchestratorFactory orchestratorFactory, ISettingsService settingsService, IMessageCenter messageCenter, List<ICatalogEntry> catalog, 
            IModuleAdministrationService moduleAdministration, ModuleContributionsUC moduleContributionsUC)
        {
            _logger = logger;
            _settingsService = settingsService;
            _orchestratorFactory = orchestratorFactory;
            _messageCenter = messageCenter;
            _catalog = catalog;

            _logUC = logUC;
            _thesaurusExplorerUC = thesaurusExplorerUC;
            _moduleDiagnosticUC = moduleDiagnosticUC;
            _moduleContributionsUC = moduleContributionsUC;

            InitializeComponent();

            string? recentFilesSetting = _settingsService.GetSetting("RecentFiles").GetAwaiter().GetResult();
            if (!string.IsNullOrWhiteSpace(recentFilesSetting))
            {
                _recentFiles.AddRange( recentFilesSetting.Split( '|', StringSplitOptions.RemoveEmptyEntries));
            }
            RefreshRecentFilesMenu();

            _messageCenter.Register(this);

            InfoHost.Content = _logUC;

            Loaded += MainWindow_Loaded;

            IconService.LoadFluentIconMap();
            MainTabs.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(CloseTabButton_Click));

            AddHandler(DragDrop.DragOverEvent, new DragEventHandler(MainWindow_DragOver), true);
            AddHandler(DragDrop.DropEvent, new DragEventHandler(MainWindow_Drop), true);
            _moduleAdministrationService = moduleAdministration;

            


        }

        #region Window management

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var themeTag = await LoadThemeTag();

            if (string.IsNullOrWhiteSpace(themeTag))
            {
                return;
            }

            _themeTag = themeTag;
            ThemeService.ApplyTheme(_themeTag);
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;

            string glyphName = WindowState == WindowState.Maximized
                ? "ic_fluent_square_multiple_24_regular"
                : "ic_fluent_maximize_24_regular";

            MaximizeButton.Content = IconService.FluentGlyph(glyphName);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            ThemeSwitch();
        }

        private async void ThemeSwitch()
        {
            switch (_themeTag)
            {
                case "Light":
                    _themeTag = "Dark";
                    ThemeButton.Content = IconService.FluentGlyph("ic_fluent_heart_circle_24_regular");
                    break;

                case "Dark":
                    _themeTag = "HighContrast";
                    ThemeButton.Content = IconService.FluentGlyph("ic_fluent_weather_sunny_24_regular");
                    break;

                case "HighContrast":
                    _themeTag = "Light";
                    ThemeButton.Content = IconService.FluentGlyph("ic_fluent_weather_moon_24_regular");
                    break;

                default:
                    return;
            }

            await SaveThemeTag(_themeTag);
            ThemeService.ApplyTheme(_themeTag);
        }

        private async Task<string?> LoadThemeTag()
        {
            var themeTag = await _settingsService.GetSetting("Theme");
            return themeTag;
        }

        private async Task SaveThemeTag(string themeTag)
        {
            await _settingsService.SaveSetting("Theme", themeTag);
        }

        #endregion


        #region New analysis

        private void MainWindow_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("FileNameW", false) &&
                !e.Data.GetDataPresent(DataFormats.FileDrop, false))
            {
                return;
            }

            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            string[]? filePaths = ResolveDroppedFilePaths(e.Data);

            if (filePaths is null)
            {
                string receivedFormats = string.Join(", ", e.Data.GetFormats(false));

                _logger.Log(LogLevel.Warning, $"Drop rejected: unsupported drag/drop data format. Expected: FileNameW or {DataFormats.FileDrop}. Received: {receivedFormats}");

                e.Handled = true;
                return;
            }

            foreach (var filePath in filePaths)
            {
                AnalyzeFile(filePath);
            }

            e.Handled = true;
        }

        private static string[]? ResolveDroppedFilePaths(IDataObject data)
        {
            string[] fileNameWPaths = data.GetData("FileNameW", false) switch
            {
                string fileName when !string.IsNullOrWhiteSpace(fileName) => new[] { fileName },
                string[] fileNames => fileNames,
                _ => Array.Empty<string>()
            };

            string[] fileDropPaths =
                data.GetData(DataFormats.FileDrop, false) as string[]
                ?? Array.Empty<string>();

            // FileNameW preserves dropped .lnk files instead of resolving their target.
            // FileDrop is preferred only for multi-file drops, because it reliably carries all selected paths.
            return (fileNameWPaths.Length, fileDropPaths.Length) switch
            {
                ( > 1, _) => fileNameWPaths,
                (_, > 1) => fileDropPaths,
                (1, _) => fileNameWPaths,
                (_, 1) => fileDropPaths,
                _ => null
            };
        }

        private void AnalyzeFile(string filePath, string? tempFolderPath = null)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(tempFolderPath))
            {
                tempFolderPath = Util.File.CreateTempFolder("Espluquer").FullName;
            }
            AnalysisContext analysisContext = new()
            {
                FilePath = filePath,
                TempFolderPath = tempFolderPath,
                StartingTag = _startingTag
            };
            

            IEngine engine = _orchestratorFactory.CreateEngine();

            AnalysisViewUC analysisView = new AnalysisViewUC(engine, _logger, analysisContext);

            AddTab(Path.GetFileName(filePath), analysisView);
            AddRecentFile(filePath);
        }

        #endregion


        #region Menu

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            MainMenu.PlacementTarget = MenuButton;
            MainMenu.Placement = PlacementMode.Bottom;
            MainMenu.IsOpen = true;
        }

        private async void MainMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem)
            {
                return;
            }

            string? menuTag = menuItem.Tag?.ToString();

            switch (menuTag)
            {
                case "OpenFiles":
                    OpenFileDialog openFileDialog = new()
                    {
                        Title = "Open file",
                        CheckFileExists = true,
                        Multiselect = true
                    };

                    if (openFileDialog.ShowDialog() != true)
                    {
                        return;
                    }

                    foreach (string filePath in openFileDialog.FileNames)
                    {
                        AnalyzeFile(filePath);
                    }

                    return;

                // Thesaurus
                case "Thesaurus.Reference":
                    // TODO
                    return;

                case "Thesaurus.Concepts":
                    AddTab("Thesaurus concepts", _thesaurusExplorerUC);
                    return;

                case "Thesaurus.ContributionMap":
                    AddTab("Thesaurus Contribution Map", _moduleContributionsUC);
                    return;

                // Modules
                case "Modules.Administration":
                    AddTab("Module Administration", _moduleDiagnosticUC);
                    return;

                case "Modules.Settings":
                    var settingsUC = new ModuleToolsUC(_logger, _moduleAdministrationService, _catalog, "IWpfSettings");
                    AddTab("Module Settings", settingsUC);
                    return;

                case "Modules.Maintenance":
                    var maintenanceUC = new ModuleToolsUC(_logger, _moduleAdministrationService, _catalog, "IWpfMaintenance");
                    AddTab("Module Maintenance", maintenanceUC);
                    return;
                /*
                {
                    ICatalogEntry? maintenanceEntry = _catalog.FirstOrDefault(entry =>
                        entry.ModuleName == "Pronom module"
                        && entry.InterfaceType == "IWpfMaintenance"
                        && entry.ClassName == "Pronom.Maintenance");

                    if (maintenanceEntry is null) return;

                    (string label, object instance)? instancePack =
                        await _moduleAdministration.CreateAdminInstance(maintenanceEntry);

                    if (instancePack?.instance is not IWpfMaintenance maintenance) return;

                    object? content = await maintenance.GetWpfMaintenance();

                    if (content is UserControl userControl)
                    {
                        AddTab(instancePack.Value.label, userControl);
                    }

                    return;

                }
                */

                //Debug
                case "Debug.LogCatalog":
                    LogCatalog(_catalog);
                    return;

                // Espluque
                case "Espluque.Settings":
                    // TODO
                    return;

                case "Espluque.Documentation":
                    // TODO
                    return;

                case "Espluque.About":
                    var aboutUC = new AboutUC();
                    AddTab("About", aboutUC);
                    return;

                // Exit
                case "Exit":
                    Close();
                    return;
            }
        }

        private void RecentFileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem ||
                menuItem.Tag is not string filePath)
            {
                return;
            }

            if (!File.Exists(filePath))
            {
                _recentFiles.RemoveAll(
                    recentFile => string.Equals(
                        recentFile,
                        filePath,
                        StringComparison.OrdinalIgnoreCase));

                RefreshRecentFilesMenu();

                _logger.Log(
                    LogLevel.Warning,
                    $"Recent file no longer exists: {filePath}");

                return;
            }

            AnalyzeFile(filePath);
        }

        #endregion


        #region tab management

        private void AddTab(string title, UserControl content)
        {
            TabItem tabItem = new TabItem
            {
                Header = title,
                Content = content
            };

            MainTabs.Items.Add(tabItem);
            MainTabs.SelectedItem = tabItem;
            UpdateMainTabsVisibility();
        }

        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is not Button button)
            {
                return;
            }

            if (button.Name != "CloseTabButton")
            {
                return;
            }

            if (button.Tag is not TabItem tabItem)
            {
                return;
            }

            if (tabItem.Content is IDisposable disposable)
            {
                disposable.Dispose();
            }

            MainTabs.Items.Remove(tabItem);
            UpdateMainTabsVisibility();
            e.Handled = true;
        }

        private void UpdateMainTabsVisibility()
        {
            MainTabs.Visibility = MainTabs.Items.Count == 0
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        #endregion


        #region Message Management

        public Task SendAsync(IMessage message)
        {
            return _messageCenter.SendAsync(message);
        }

        public Task HandleAsync(IMessage message)
        {
            switch (message.MessageType)
            {
                case MessageTypeEnum.Analyze:
                    var analyzeFilePath = message.Payload.FirstOrDefault(item => item.Key == "FilePath").Value;
                    if (!File.Exists(analyzeFilePath))
                    {
                        _logger.Log(LogLevel.Error, $"File {analyzeFilePath} doesn't exist.");
                        return Task.CompletedTask;
                    }
                    AnalyzeFile(analyzeFilePath);
                    break;

                case MessageTypeEnum.ExtractAndAnalyze:
                    var containerFilePath = message.Payload.FirstOrDefault(item => item.Key == "FilePath").Value;
                    var internalPath = message.Payload.FirstOrDefault(item => item.Key == "InternalPath").Value;

                    var tempFolderPath = Util.File.CreateTempFolder("Espluquer").FullName;
                    var extractedFilename = Path.GetFileName(internalPath);
                    var extractedFilePath = Path.Combine(tempFolderPath, extractedFilename);

                    var extractFileResult = SevenZip.Services.SevenZipService.ExtractEntryToFile(containerFilePath, internalPath, extractedFilePath);

                    if (!extractFileResult.IsSuccess)
                    {
                        _logger.Log(LogLevel.Error, $"Cannot extract {internalPath} from {containerFilePath}: {extractFileResult?.Error?.Message}");
                        return Task.CompletedTask;
                    }
                    AnalyzeFile(extractedFilePath, tempFolderPath);
                    break;
            }

            return Task.CompletedTask;
        }

        #endregion


        #region Recent file

        private void AddRecentFile(string filePath)
        {
            string fullPath = Path.GetFullPath(filePath);

            _recentFiles.RemoveAll( recentFile => string.Equals( recentFile, fullPath, StringComparison.OrdinalIgnoreCase));

            _recentFiles.Insert(0, fullPath);

            if (_recentFiles.Count > 10)
            {
                _recentFiles.RemoveRange( 10, _recentFiles.Count - 10);
            }

            RefreshRecentFilesMenu();

            _settingsService.SaveSetting( "RecentFiles", string.Join('|', _recentFiles));
        }

        private void RefreshRecentFilesMenu()
        {
            RecentFilesMenu.Items.Clear();

            foreach (string filePath in _recentFiles)
            {
                MenuItem recentFileMenuItem = new()
                {
                    Header = Path.GetFileName(filePath),
                    Tag = filePath,
                    ToolTip = filePath
                };

                recentFileMenuItem.Click += RecentFileMenuItem_Click;

                RecentFilesMenu.Items.Add(recentFileMenuItem);
            }

            RecentFilesMenu.IsEnabled = _recentFiles.Count > 0;
        }

        #endregion

        protected override void OnClosed(EventArgs e)
        {
            _messageCenter.Unregister(this);
            string tempFolder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Espluquer");
            Directory.Delete(tempFolder, recursive: true);
            base.OnClosed(e);
        }

        private void LogCatalog(List<ICatalogEntry> catalog)
        {
            _logger.Log(
                LogLevel.Debug,
                $"{"ASM",-4} | {"MODULE@VERSION",-16} | {"INTERFACE",-16} | {"CLASS",-30} | {"LABEL",-16} | {"TAGS",-16} | {"FILE",-20}");

            _logger.Log(LogLevel.Debug, new string('-', 136));

            foreach (ICatalogEntry entry in catalog)
            {
                string assemblyState = entry.Assembly is null ? "NULL" : "OK";
                string module = $"{entry.ModuleName}@{entry.ModuleVersion}".PadRight(16)[..16];
                string interfaceType = entry.InterfaceType.PadRight(16)[..16];
                string className = entry.ClassName.PadRight(30)[..30];
                string label = entry.Label.PadRight(16)[..16];
                string tags = string.Join(", ", entry.Tags).PadRight(16)[..16];
                string assemblyFile = Path.GetFileName(entry.AssemblyPath).PadRight(20)[..20];

                _logger.Log(
                    LogLevel.Debug,
                    $"{assemblyState,-4} | {module} | {interfaceType} | {className} | {label} | {tags} | {assemblyFile}");
            }
        }
    }
}
