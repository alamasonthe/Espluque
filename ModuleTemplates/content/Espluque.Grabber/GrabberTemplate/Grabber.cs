using Espluque.Contracts.Interfaces;
using Espluque.Contracts.MessageInterfaces;
using Espluque.Contracts.Ports;
using Espluque.Contracts.ModuleInterfaces.Contributions;


namespace GrabberTemplate
{
    public class Grabber: IGrabber
    {
        private readonly IMessageCenter _messageCenter;
        private readonly Espluque.Contracts.Ports.ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        public Grabber(IMessageCenter messageCenter,
            Espluque.Contracts.Ports.ILogger logger,
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
            List<KeyValuePair<string, string>> infos = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("Key1", "Value1"),
                    new KeyValuePair<string, string>("Key2", "Value2")
                };

            return Task.FromResult(infos);
        }
    }
}
