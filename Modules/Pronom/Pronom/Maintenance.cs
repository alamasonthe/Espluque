using Espluque.Contracts.Contributions.Types;
using Espluque.Contracts.CrossCutting;

namespace Pronom
{
    public class Maintenance: IWpfMaintenance
    {
        private readonly IMessageCenter _messageCenter;
        private readonly ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        private readonly string _referentiel = "Pronom";

        public Maintenance(IMessageCenter messageCenter,
            ILogger logger,
            ISettingsService settingsService,
            IEntityFactory entityFactory)
        {
            _messageCenter = messageCenter;
            _logger = logger;
            _settingsService = settingsService;
            _entityFactory = entityFactory;
        }

        public async Task<object?> GetWpfMaintenance()
        {
            return new UserControls.MaintenanceUC(_logger, _settingsService);
        }
    }
}
