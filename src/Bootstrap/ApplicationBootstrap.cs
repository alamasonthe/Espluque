using Espluque.Application.Catalog;
using Espluque.Application.Contributions;
using Espluque.Application.CrossCutting;
using Espluque.Application.CrossCutting.MessageBus;
using Espluque.Application.Modules;
using Espluque.Application.Thesaurus;
using Espluque.Contracts.Interfaces;
using Espluque.Contracts.MessageInterfaces;
using Espluque.Contracts.ModuleInterfaces;
using Espluque.Contracts.Ports;
using EspluqueSqlite.Thesaurus;
using LuceneSearch;
using Microsoft.Extensions.DependencyInjection;

namespace Bootstrap
{
    public static class ApplicationBootstrap
    {
        public static async Task<IServiceCollection> Initialize(this IServiceCollection services)
        {

            services.AddSingleton<IEntityFactory,Factory>();

            ISettingsService settingsService = new SettingsJson.SettingsService();
            services.AddSingleton(settingsService);
            services.AddSingleton<Espluque.Contracts.Ports.ILogger, MiniFileLogger.Logger>();

            services.AddSingleton<IMessageCenter, MessageCenter>();

            string modulesRootPath = Path.Combine( AppContext.BaseDirectory, "Modules");
            CatalogService catalogService = new(settingsService);
            List<ICatalogEntry> moduleCatalog = await catalogService.BuildAsync(modulesRootPath);
            services.AddSingleton(moduleCatalog);

            await ModuleService.LoadModuleDependenciesAsync(moduleCatalog);

            services.AddSingleton<IModuleAdministrationService, ModuleAdministrationService>();

            services.AddTransient<Espluque.Application.Workflow.Orchestrator>();
            services.AddSingleton<Espluque.Contracts.Workflow.IOrchestratorFactory, Espluque.Application.Workflow.OrchestratorFactory>();

            services.AddSingleton<Espluque.Contracts.Ports.IThesaurusSource, ThesaurusRepository>();
            services.AddSingleton<IThesaurusService, ThesaurusService>();

            services.AddSingleton<ISearchService, SearchService>();

            services.AddSingleton<Espluque.Contracts.ModuleInterfaces.IModuleService, ModuleService>();
            services.AddSingleton<Espluque.Contracts.ModuleInterfaces.IModuleDiagService, ModuleDiagService>();

            services.AddSingleton<IContributionSettingsService, ContributionSettingsService>();

            return services;
        }
    }
}
