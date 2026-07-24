using Espluque.Contracts.Interfaces;
using Espluque.Contracts.MessageInterfaces;
using Espluque.Contracts.ModuleInterfaces;
using Espluque.Contracts.Ports;

namespace Espluque.Application.ModuleManager.Services
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
