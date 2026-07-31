using Bootstrap;
using Espluquer.Services;
using Espluquer.UserControls.Modules;
using Espluquer.UserControls.ResultModeling;
using Espluquer.UserControls.Shell;
using Espluquer.UserControls.Thesaurus;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

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
            services.AddSingleton<ConceptUC>();
            services.AddSingleton<ModuleAdminUC>();
            services.AddSingleton<ContributionMapUC>();
            services.AddSingleton<ReferenceUC>();
            services.AddSingleton<ResultModelsUC>();

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