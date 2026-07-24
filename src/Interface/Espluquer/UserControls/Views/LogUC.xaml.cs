using System.Text;
using System.Windows;
using System.Windows.Controls;
using Espluque.Contracts.Ports;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Espluquer.UserControls.Views
{
    public partial class LogUC : UserControl
    {
        private readonly ILogger _miniLogger;
        private readonly List<(string Line, LogLevel? Level)> _lines = [];

        public LogUC(ILogger miniLogger)
        {
            InitializeComponent();

            _miniLogger = miniLogger;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            LogLevelSlider.ValueChanged += OnLogLevelChanged;
        }

        private LogLevel SelectedLevel => (LogLevel)(int)LogLevelSlider.Value;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _miniLogger.LineLogged += OnLineLogged;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _miniLogger.LineLogged -= OnLineLogged;
        }

        private void OnLineLogged(string line)
        {
            Dispatcher.InvokeAsync(() =>
            {
                LogLevel? level = GetLogLevel(line);

                _lines.Add((line, level));

                if (ShouldDisplay(level))
                {
                    LogTextBox.AppendText(line);
                    LogTextBox.AppendText(Environment.NewLine);
                    LogTextBox.ScrollToEnd();
                }
            });
        }

        private void OnLogLevelChanged(
            object sender,
            RoutedPropertyChangedEventArgs<double> e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            StringBuilder text = new();

            foreach ((string line, LogLevel? level) in _lines)
            {
                if (ShouldDisplay(level))
                {
                    text.AppendLine(line);
                }
            }

            LogTextBox.Text = text.ToString();
            LogTextBox.ScrollToEnd();
        }

        private bool ShouldDisplay(LogLevel? level)
        {
            return level is null ||
                   (int)level.Value >= (int)SelectedLevel;
        }

        private static LogLevel? GetLogLevel(string line)
        {
            int firstTab = line.IndexOf('\t');

            if (firstTab < 0)
            {
                return null;
            }

            int secondTab = line.IndexOf('\t', firstTab + 1);

            string levelText = secondTab < 0
                ? line[(firstTab + 1)..]
                : line[(firstTab + 1)..secondTab];

            return Enum.TryParse(
                levelText.Trim(),
                true,
                out LogLevel level)
                    ? level
                    : null;
        }

        private void LogFilterToggleButton_Click(object sender, RoutedEventArgs e)
        {
            LogFilterPanel.Visibility =
                LogFilterPanel.Visibility == Visibility.Visible
                    ? Visibility.Collapsed
                    : Visibility.Visible;

            LogTextBox.ScrollToEnd();
        }

    }
}