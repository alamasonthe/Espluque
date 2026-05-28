using Bootstrap;
using Microsoft.Extensions.DependencyInjection;
using Modele1.UserControls;
using System.Configuration;
using System.Data;
using System.Windows;

namespace Modele1
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();

            services.Initialize();

            services.AddSingleton<LogUC>();
            services.AddSingleton<DyneUC>();
            services.AddSingleton<PronomUC>();
            services.AddSingleton<MainWindow>();

            _serviceProvider = services.BuildServiceProvider();

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();

            base.OnExit(e);
        }
    }

}
