using Espluque.Contracts.Catalog;
using Espluque.Contracts.Contributions.Types;
using Espluque.Contracts.CrossCutting;
using Espluque.Contracts.Workflow;
using Espluquer.Services;
using Espluquer.Usercontrols.Shell;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.Loader;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Espluquer.UserControls.Shell
{
    public partial class AnalysisViewUC : UserControl, IDisposable
    {
        private IAnalysisContext _analysisContext;

        private readonly List<ICatalogEntry> _catalog = [];

        private readonly IOrchestrator _orchestrator;
        private readonly ILogger _logger;
        private bool _isDisposed;

        private readonly ObservableCollection<TabItem> _analysisTabItems = [];
        private readonly List<(string Label, IWpfViewer Viewer)> _viewerQueue = [];

        #region Lifecycle

        public AnalysisViewUC(IOrchestratorFactory orchestratorFactory, ILogger logger, IAnalysisContext analysisContext, List<ICatalogEntry> catalog)
        {
            _logger = logger;
            _analysisContext = analysisContext;
            _orchestrator = orchestratorFactory.CreateOrchestrator();
            _catalog = catalog;

            InitializeComponent();
            FilePathTextbox.Text = analysisContext.FilePath;
            AnalysisTabControl.ItemsSource = _analysisTabItems;
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            await AnalyzeFileAsync(_analysisContext);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            foreach (TabItem tabItem in _analysisTabItems)
            {
                if (tabItem.Content is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            GC.SuppressFinalize(this);
        }

        #endregion


        #region Analysis Workflow

        private async Task AnalyzeFileAsync(IAnalysisContext analysisContext)
        {
            _orchestrator.AnalyserMessageEvent += ReceiveAnalyserMessage;

            StartAnalysisProgressAnimation();

            await Task.Run(() => _orchestrator.ProcessAsync(_catalog, analysisContext, "IWpfViewer"));
        }

        private async void ReceiveAnalyserMessage(IAnalysisMessage message)
        {
            try
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    switch (message.MessageType)
                    {
                        case AnalysisMessageTypeEnum.AnalysisCompleted:
                            _orchestrator.AnalyserMessageEvent -= ReceiveAnalyserMessage;
                            StopAnalysisProgressAnimation();
                            await ProcessViewerBacklogAsync();
                            break;

                        case AnalysisMessageTypeEnum.DetectorResult:
                            break;

                        case AnalysisMessageTypeEnum.GrabberResult:
                            if (message.Information is not null)
                            {
                                AddTabItem(
                                    message.Information.Label,
                                    new ListRichTextBoxUC(message.Information.Information),
                                    "IGrabber");
                            }
                            break;

                        case AnalysisMessageTypeEnum.FusionerSummary:
                            if (message.Information is not null)
                            {
                                AddTabItem(
                                    message.Information.Label,
                                    new ListRichTextBoxUC(message.Information.Information),
                                    "IFusioner");
                            }
                            break;

                        case AnalysisMessageTypeEnum.ViewerUC:
                            if (message.ViewerUC is IWpfViewer viewer)
                            {
                                if (string.IsNullOrWhiteSpace(message.Label))
                                {
                                    message.Label = "Viewer";
                                }

                                _viewerQueue.Add((message.Label, viewer));
                            }
                            break;
                    }
                }).Task.Unwrap();
            }
            catch (Exception ex)
            {
                _logger.Log(
                    Microsoft.Extensions.Logging.LogLevel.Error,
                    $"Espluquer message handling: {ex.GetBaseException().Message}");
            }
        }

        private async Task ProcessViewerBacklogAsync()
        {
            string formattedFileName = System.IO.Path.GetFileName(_analysisContext.FilePath).PadRight(35);
            _logger.Log(Microsoft.Extensions.Logging.LogLevel.Debug, $"{formattedFileName}\tViewer Tasks queued: {_viewerQueue.Count}");

            foreach ((string label, IWpfViewer viewer) in _viewerQueue)
            {
                await DisplayViewerAsync(label, viewer);
            }

            _viewerQueue.Clear();
            
            _logger.Log(Microsoft.Extensions.Logging.LogLevel.Debug, $"{formattedFileName}\tViewer Task done");
        }

        #endregion


        #region Content Display

        private async Task DisplayViewerAsync(string label, IWpfViewer viewer)
        {
            try
            {
                AssemblyLoadContext? loadContext = AssemblyLoadContext.GetLoadContext(viewer.GetType().Assembly);

                object? result;

                if (loadContext is null)
                {
                    result = await viewer.GetViewer(_analysisContext);
                }
                else
                {
                    using (loadContext.EnterContextualReflection())
                    {
                        result = await viewer.GetViewer(_analysisContext);
                    }
                }

                if (result is null)
                {
                    _logger.Log(Microsoft.Extensions.Logging.LogLevel.Debug, $"Espluquer Viewer display: viewer unavailable ({label})");
                    return;
                }

                if (result is not UserControl userControl)
                {
                    _logger.Log(Microsoft.Extensions.Logging.LogLevel.Debug, $"Espluquer Viewer display: returned object is not a UserControl ({result.GetType().FullName})");
                    return;
                }

                AddTabItem(label, userControl, "IWpfViewer");
            }
            catch (Exception ex)
            {
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"Espluquer Viewer display: {ex.ToString().ReplaceLineEndings(" | ")}");
            }

        }

        private void AddTabItem(string label, object content, string interfaceType)
        {
            TextBlock icon = new()
            {
                Text = ModuleTestService.GetContributionIcon(interfaceType),
                FontSize = 16,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            icon.SetResourceReference(TextBlock.FontFamilyProperty, "FluentIcons");

            StackPanel header = new()
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            header.Children.Add(icon);
            header.Children.Add(new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center
            });

            _analysisTabItems.Add(new TabItem
            {
                Header = header,
                Content = content
            });

            if (AnalysisTabControl.SelectedItem is null)
            {
                AnalysisTabControl.SelectedIndex = 0;
            }
        }

        #endregion


        #region View Controls

        private void OpenFileLocationButton_Click(object sender, RoutedEventArgs e)
        {
            if (!System.IO.File.Exists(_analysisContext.FilePath))
            {
                return;
            }

            string arguments = $"/select,\"{_analysisContext.FilePath}\"";

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = arguments,
                UseShellExecute = true
            });
        }

        private void StartAnalysisProgressAnimation()
        {
            AnalysisCompletedTextBlock.Visibility = Visibility.Collapsed;
            AnalysisProgressIcon.Visibility = Visibility.Visible;

            DoubleAnimation rotationAnimation = new()
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(2),
                RepeatBehavior = RepeatBehavior.Forever
            };

            AnalysisProgressIconRotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotationAnimation);
        }

        private void StopAnalysisProgressAnimation()
        {
            AnalysisProgressIconRotateTransform.BeginAnimation(RotateTransform.AngleProperty, null);
            AnalysisProgressIconRotateTransform.Angle = 0;

            AnalysisProgressIcon.Visibility = Visibility.Collapsed;
            AnalysisCompletedTextBlock.Visibility = Visibility.Visible;
        }

        #endregion

    }
}