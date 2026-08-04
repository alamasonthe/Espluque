using Espluque.Contracts.Entities;
using Espluque.Contracts.Interfaces;
using Espluque.Contracts.MessageInterfaces;
using Espluque.Contracts.ModuleInterfaces.Contributions;
using Espluque.Contracts.Ports;
using SoftwarePackage.Entities;
using SoftwarePackage.Mapper;
using SoftwarePackage.Services;
using System.IO;
using System.Text.Json;

namespace SoftwarePackage
{
    public class Fusioner : IFusioner
    {
        private readonly IMessageCenter _messageCenter;
        private readonly ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        public Fusioner()
        {

        }

        public Fusioner(IMessageCenter messageCenter,
            ILogger logger,
            ISettingsService settingsService,
            IEntityFactory entityFactory)
        {
            _messageCenter = messageCenter;
            _logger = logger;
            _settingsService = settingsService;
            _entityFactory = entityFactory;
        }

        public async Task<IAssertion> Fuse(AnalysisContext analysisContext)
        {
            string filename = Path.GetFileName(analysisContext.FilePath);
            string formattedFilename = filename.PadRight(35);

            List<MapLine> mappings = await MapSource.Load();
            Package package = FusionMapper<Package>.Map(analysisContext, mappings, _logger);

            string? formatTag = analysisContext.TagHistory.LastOrDefault();
            package.InstallerType = formatTag;

            CommandCompletion commandCompletion = new(_logger);

            CommandLineTemplate? commandLineTemplate = await commandCompletion.GetTemplate(filename, formatTag);

            if (commandLineTemplate != null)
            {
                package.InstallCommand = commandCompletion.ReplaceVariables(commandLineTemplate.InstallCommand, analysisContext, "Windows App Package infos");
                package.InstallArguments = commandCompletion.ReplaceVariables(commandLineTemplate.InstallArguments, analysisContext, "Windows App Package infos");
                package.UninstallCommand = commandCompletion.ReplaceVariables(commandLineTemplate.UninstallCommand, analysisContext, "Windows App Package infos");
                package.UninstallArguments = commandCompletion.ReplaceVariables(commandLineTemplate.UninstallArguments, analysisContext, "Windows App Package infos");

            }

            var assertion = _entityFactory.CreateAssertion(
                "Software Package module",
                "Package Fusioner",
                "Software Package",
                JsonSerializer.Serialize(package),
                package.GetTextProperties()
                );

            return assertion;
        }
    }
}
