using Espluque.Contracts.Interfaces;
using Espluque.Contracts.Contributions.Types;
using Espluque.Contracts.MessageInterfaces;
using Espluque.Contracts.Ports;
using Espluque.Contracts.Workflow;
using Espluque.Fusioner.Entities;
using Espluque.Contracts.Entities;
using System.Text.Json;

namespace espluque-fusioner
{
    public class Fusioner : IFusioner
    {
        private readonly IMessageCenter _messageCenter;
        private readonly Espluque.Contracts.Ports.ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        private readonly string _referentiel = "TemplateReferentiel";

        public Fusioner(IMessageCenter messageCenter,
            Espluque.Contracts.Ports.ILogger logger,
            ISettingsService settingsService,
            IEntityFactory entityFactory)
        {
            _messageCenter = messageCenter;
            _logger = logger;
            _settingsService = settingsService;
            _entityFactory = entityFactory;
        }

        public async Task<IAssertion> Fuse(IAnalysisContext analysisContext)
        {
            string filename = Path.GetFileName(analysisContext.FilePath);
            string formattedFilename = filename.PadRight(35);

            MyEntity MyEntity = new();

            var assertion = _entityFactory.CreateAssertion(
                "Software Package module",
                "Package Fusioner",
                "Software Package",
                JsonSerializer.Serialize(MyEntity),
                MyEntity.GetTextProperties()
                );

            return assertion;
        }
    }
}
