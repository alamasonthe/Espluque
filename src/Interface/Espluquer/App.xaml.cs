using Bootstrap;
using Espluquer.UserControls.Components;
using Espluquer.UserControls.Views;
using Microsoft.Extensions.DependencyInjection;
using Espluquer.Services;
using System.Windows;
using Espluquer.UserControls.Parameters;

namespace Espluquer
{
    public partial class App : Application
    {
        private IDisposable? _serviceProvider;

        static App()
        {
            AssemblyResolver.Register();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();

            await services.Initialize();

            services.AddSingleton<LogUC>();
            services.AddSingleton<ThesaurusExplorerUC>();
            services.AddSingleton<ModuleDiagnosticUC>();
            services.AddSingleton<ModuleContributionsUC>();
            services.AddSingleton<ReferenceUC>();

            services.AddSingleton<WebView2Configuration>();
            services.AddSingleton<SqliteConfiguration>();

            services.AddSingleton<MainWindow>();

            ServiceProvider serviceProvider = services.BuildServiceProvider();
            _serviceProvider = serviceProvider;

            serviceProvider.GetRequiredService<WebView2Configuration>().Configure();
            serviceProvider.GetRequiredService<SqliteConfiguration>().Configure();

            var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();

            base.OnExit(e);
        }
    }
}