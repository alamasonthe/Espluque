using DyneSqlite;
using Espluque.Application.Services;
using Espluque.Application.Entities;
using Espluque.Contracts.Ports;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using PronomSqlite;
using Espluque.Contracts.Interfaces;

namespace Bootstrap
{
    public static class ApplicationBootstrap
    {
        public static IServiceCollection Initialize(this IServiceCollection services)
        {

            services.AddSingleton<IEntityFactory,Factory>();

            services.AddSingleton<Espluque.Contracts.Ports.ILogger,MiniFileLogger.Logger>();
            services.AddSingleton<Espluque.Contracts.Orchestrators.IAnalyzer,Espluque.Application.Orchestrators.Analyzer>();
            services.AddSingleton<IFileFormatService,PronomService>();

            services.AddSingleton<IDyneExtensionRepository, ExtensionRepository>();
            services.AddSingleton<IDyneCategoryRepository, CategoryRepository>();
            services.AddSingleton<IDyneCategoryExtensionRepository, CategoryExtensionRepository>();
            services.AddSingleton<IDyneFileSource, DyneFileSource>();
            services.AddSingleton<DyneService>();

            services.AddSingleton<IImportFileSignatureRepository, ImportFileSignatureRepository>();
            services.AddSingleton<IPronomRepository, PronomRepository>();
            services.AddSingleton<PronomRepository>();
            services.AddSingleton<IFileSignatureSource, FileSignatureSource>();
            services.AddSingleton<PronomService>();
            services.AddSingleton<IFileFormatService>( serviceProvider => serviceProvider.GetRequiredService<PronomService>());
            services.AddSingleton<IFileFormatService>( serviceProvider => serviceProvider.GetRequiredService<PronomService>());

            string databasePath = Path.Combine(AppContext.BaseDirectory, "espluque.db");
            string connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath
            }.ToString();
            services.AddSingleton(new DyneSqlite.DbConnectionFactory(connectionString));
            services.AddSingleton(new PronomSqlite.DbConnectionFactory(connectionString));


            return services;
        }
    }
}