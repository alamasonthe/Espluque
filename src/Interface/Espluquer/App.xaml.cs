using Bootstrap;
using Espluque.Contracts.Interfaces;
using Espluque.Contracts.Ports;
using Espluquer.Services;
using Espluquer.UserControls.Modules;
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
            services.AddTransient<ConceptSearchUC>();

            services.AddSingleton<WebView2Configuration>();
            services.AddSingleton<SqliteConfiguration>();

            services.AddSingleton<MainWindow>();

            ServiceProvider serviceProvider = services.BuildServiceProvider();
            _serviceProvider = serviceProvider;

            serviceProvider.GetRequiredService<WebView2Configuration>().Configure();
            serviceProvider.GetRequiredService<SqliteConfiguration>().Configure();

            ISearchService searchService = serviceProvider.GetRequiredService<ISearchService>();
            IThesaurusService thesaurusService = serviceProvider.GetRequiredService<IThesaurusService>();
            await searchService.Index(thesaurusService);

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