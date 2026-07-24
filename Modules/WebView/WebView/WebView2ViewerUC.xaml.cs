using System.Windows;
using System.Windows.Controls;

namespace WebView
{
    public partial class WebView2ViewerUC : UserControl
    {
        private readonly string? _filePath;
        public WebView2ViewerUC(string filePath)
        {
            InitializeComponent();

            _filePath = filePath;
            Loaded += WebView2ViewerUC_Loaded;
        }

        private async void WebView2ViewerUC_Loaded(object sender, RoutedEventArgs e)
        {
            if (!System.IO.File.Exists(_filePath))
            {
                return;
            }

            await GraphWebView.EnsureCoreWebView2Async();

            string fileUri = new Uri(_filePath).AbsoluteUri;

            GraphWebView.CoreWebView2.Navigate(fileUri);
        }
    }
}
