using System.Windows.Controls;
using Espluque.Contracts.Ports;

namespace Modele1.UserControls
{
    /// <summary>
    /// Logique d'interaction pour LogUC.xaml
    /// </summary>
    public partial class LogUC : UserControl
    {
        private readonly ILogger _miniLogger;

        public LogUC(ILogger miniLogger)
        {
            InitializeComponent();

            _miniLogger = miniLogger;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _miniLogger.LineLogged += OnLineLogged;
        }

        private void OnUnloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _miniLogger.LineLogged -= OnLineLogged;
        }

        private void OnLineLogged(string line)
        {
            Dispatcher.InvokeAsync(() =>
            {
                LogTextBox.Text += line + Environment.NewLine;
                LogTextBox.ScrollToEnd();
            });
        }
    }
}
