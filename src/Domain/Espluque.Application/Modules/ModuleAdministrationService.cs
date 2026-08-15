using Espluque.Application.Contributions;
using Espluque.Contracts.Catalog;
using Espluque.Contracts.CrossCutting;
using Espluque.Contracts.Modules;

namespace Espluque.Application.Modules
{
    public class ModuleAdministrationService : IModuleAdministrationService
    {
        private readonly IMessageCenter _messageCenter;
        private readonly ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        public ModuleAdministrationService(IMessageCenter messageCenter,
            ILogger logger,
            ISettingsService settingsService,
            IEntityFactory entityFactory)
        {
            _messageCenter = messageCenter;
            _logger = logger;
            _settingsService = settingsService;
            _entityFactory = entityFactory;
        }

        public async Task<(string label, object instance)?> CreateAdminInstance(ICatalogEntry entry)
        {
            (string label, object instance)? instancePack = InstanceBuilder.CreateInstance(entry, _messageCenter, _logger, _settingsService, _entityFactory);
            return instancePack;
        }
    }
}
