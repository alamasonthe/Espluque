using Espluque.Contracts.Entities;
using Espluque.Contracts.Interfaces;
using Espluque.Contracts.MessageInterfaces;
using Espluque.Contracts.ModuleInterfaces.Contributions;
using Espluque.Contracts.Ports;
using SoftwarePackage.Mapper;
using System.Text.Json;
using SoftwarePackage.Entities;

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
            List<MapLine> mappings = await MapSource.Load();
            Package package = FusionMapper<Package>.Map(analysisContext, mappings, _logger);

            package.InstallerType = analysisContext.TagHistory.LastOrDefault();

            // TODO: renseigner Package InstallerCommand, InstallerParameters, UninstallerCommand, UninstallerParameters 

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
