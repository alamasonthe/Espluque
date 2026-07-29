using Espluque.Application.Entities;
using Espluque.Contracts.Entities;
using Espluque.Contracts.Enums;
using Espluque.Contracts.Interfaces;
using Espluque.Contracts.Orchestrators;
using Espluque.Contracts.Ports;
using Espluquer.Usercontrols.Shell;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Runtime.Loader;
using Espluque.Contracts.ModuleInterfaces.Contributions;

namespace Espluquer.UserControls.Shell
{
    public partial class AnalysisViewUC : UserControl, IDisposable
    {
        private AnalysisContext _analysisContext;
        private List<KeyValuePair<string, string>>? _fileProperties;
        private readonly List<TaskRequest> _taskRequests = [];

        private readonly IEngine _engine;
        private readonly ILogger _logger;
        private bool _isDisposed;

        private readonly ObservableCollection<TabItem> _analysisTabItems = [];
        private readonly List<(string Label, IWpfViewer Viewer)> _viewerQueue = [];

        #region Lifecycle

        public AnalysisViewUC(IEngine engine, ILogger logger, AnalysisContext analysisContext)
        {
            _engine = engine;
            _logger = logger;
            _analysisContext = analysisContext;

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

        private async Task AnalyzeFileAsync(AnalysisContext analysisContext)
        {
            _engine.AnalyserMessageEvent += ReceiveAnalyserMessage;

            StartAnalysisProgressAnimation();

            await Task.Run(() => _engine.AnalyzeFileAsync(analysisContext));
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
                            _engine.AnalyserMessageEvent -= ReceiveAnalyserMessage;
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
                                    new ListRichTextBoxUC(message.Information.Information));
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

        private async Task<object?> CreateViewerAsync( IWpfViewer viewer, AssemblyLoadContext? loadContext)
        {
            if (loadContext is null)
            {
                return await viewer.GetViewer(_analysisContext);
            }

            using (loadContext.EnterContextualReflection())
            {
                return await viewer.GetViewer(_analysisContext);
            }
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

                AddTabItem(label, userControl);
            }
            catch (Exception ex)
            {
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"Espluquer Viewer display: {ex.ToString().ReplaceLineEndings(" | ")}");
            }

        }

        private void AddTabItem(string label, object content)
        {
            _analysisTabItems.Add(new TabItem
            {
                Header = label,
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