using Espluque.Application.Entities;
using Espluque.Application.MessageBus.Services;
using Espluque.Application.ModuleManager.Services;
using Espluque.Application.Thesaurus.Services;
using Espluque.Contracts.Interfaces;
using Espluque.Contracts.MessageInterfaces;
using Espluque.Contracts.ModuleInterfaces;
using EspluqueSqlite.Thesaurus;
using Microsoft.Extensions.DependencyInjection;

namespace Bootstrap
{
    public static class ApplicationBootstrap
    {
        public static async Task<IServiceCollection> Initialize(this IServiceCollection services)
        {

            services.AddSingleton<IEntityFactory,Factory>();

            services.AddSingleton<Espluque.Contracts.Ports.ISettingsService, SettingsJson.SettingsService>();
            services.AddSingleton<Espluque.Contracts.Ports.ILogger, MiniFileLogger.Logger>();

            services.AddSingleton<IMessageCenter, MessageCenter>();

            string modulesRootPath = Path.Combine( AppContext.BaseDirectory, "Modules");
            List<ICatalogEntry> moduleCatalog = await CatalogService.BuildAsync(modulesRootPath);
            services.AddSingleton(moduleCatalog);

            await ModuleService.LoadModuleDependenciesAsync(moduleCatalog);

            services.AddSingleton<IModuleAdministrationService, ModuleAdministrationService>();

            services.AddTransient<Espluque.Application.Workflow.Orchestrator>();
            services.AddSingleton<Espluque.Contracts.Workflow.IOrchestratorFactory, Espluque.Application.Workflow.OrchestratorFactory>();

            services.AddSingleton<Espluque.Contracts.Ports.IThesaurusSource, ThesaurusRepository>();
            services.AddSingleton<IThesaurusService, ThesaurusService>();

            services.AddSingleton<Espluque.Contracts.ModuleInterfaces.IModuleService, ModuleService>();
            services.AddSingleton<Espluque.Contracts.ModuleInterfaces.IModuleDiagnosticService, ModuleDiagnosticService>();

            return services;
        }
    }
}
