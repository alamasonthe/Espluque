using Microsoft.Web.WebView2.Core;
using Espluque.Contracts.CrossCutting;

namespace Espluquer.Services
{
    internal class WebView2Configuration
    {
        private bool _isConfigured;

        internal void Configure()
        {
            if (_isConfigured)
            {
                return;
            }

            _isConfigured = true;

            CoreWebView2Environment.SetLoaderDllFolderPath(RuntimePaths.NativeWebView2DirectoryPath);
        }
    }
}
