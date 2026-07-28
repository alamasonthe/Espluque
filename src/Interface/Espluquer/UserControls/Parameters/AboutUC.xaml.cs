using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Resources;

namespace Espluquer.UserControls.Components
{
    public partial class AboutUC : UserControl
    {
        private const string RepositoryUrl =
            "https://github.com/alamasonthe/Espluque";

        public AboutUC()
        {
            InitializeComponent();

            VersionRun.Text = $"Version {GetApplicationVersion()}";

            RepositoryHyperlink.NavigateUri = new Uri(RepositoryUrl);
            RepositoryHyperlink.Inlines.Add(new Run(RepositoryUrl));

            ApplicationLogo.Source = LoadLargestIconFrame();
        }

        private static string GetApplicationVersion()
        {
            Assembly assembly =
                Assembly.GetEntryAssembly()
                ?? Assembly.GetExecutingAssembly();

            string? informationalVersion = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

            if (!string.IsNullOrWhiteSpace(informationalVersion))
            {
                return informationalVersion.Split('+')[0];
            }

            return assembly.GetName().Version?.ToString(3)
                   ?? "Unknown";
        }

        private void RepositoryHyperlink_RequestNavigate(
            object sender,
            RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });

            e.Handled = true;
        }

        private static BitmapFrame LoadLargestIconFrame()
        {
            Uri iconUri = new(
                "/Espluquer.ico",
                UriKind.Relative);

            StreamResourceInfo resource =
                Application.GetResourceStream(iconUri)
                ?? throw new InvalidOperationException(
                    $"Resource not found: {iconUri}");

            using Stream stream = resource.Stream;

            IconBitmapDecoder decoder = new(
                stream,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);

            BitmapFrame frame = decoder.Frames
                .OrderByDescending(frame => frame.PixelWidth)
                .ThenByDescending(frame => frame.PixelHeight)
                .First();

            frame.Freeze();

            return frame;
        }
    }
}