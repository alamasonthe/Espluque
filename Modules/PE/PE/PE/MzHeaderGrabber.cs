using Espluque.Contracts.Contributions.Types;
using Espluque.Contracts.CrossCutting;
using Espluque.Contracts.Workflow;
using PE.Entities;
using PE.Extensions;

namespace PE
{
    public class MzHeaderGrabber : IGrabber
    {
        private readonly IMessageCenter _messageCenter;
        private readonly Espluque.Contracts.CrossCutting.ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        public MzHeaderGrabber(
            IMessageCenter messageCenter,
            Espluque.Contracts.CrossCutting.ILogger logger,
            ISettingsService settingsService,
            IEntityFactory entityFactory)
        {
            _messageCenter = messageCenter;
            _logger = logger;
            _settingsService = settingsService;
            _entityFactory = entityFactory;
        }

        public Task<List<KeyValuePair<string, string>>> Grab(IAnalysisContext analysisContext)
        {
            PeDosMzHeader header = new(analysisContext.FilePath ?? string.Empty, _logger);
            List<KeyValuePair<string, string>> infos = header.ToGrabberList();

            return Task.FromResult(infos);
        }
    }
}