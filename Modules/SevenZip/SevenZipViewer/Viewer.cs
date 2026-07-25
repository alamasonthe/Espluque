using Espluque.Contracts.Entities;
using Espluque.Contracts.Interfaces;
using Espluque.Contracts.MessageInterfaces;
using Espluque.Contracts.ModuleInterfaces.Contributions;
using Espluque.Contracts.Ports;
using SevenZip.Services;

namespace SevenZipViewer
{
    public class Viewer : IWpfViewer
    {
        private readonly IMessageCenter _messageCenter;
        private readonly ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        public Viewer(IMessageCenter messageCenter,
            ILogger logger,
            ISettingsService settingsService,
            IEntityFactory entityFactory)
        {
            _messageCenter = messageCenter;
            _logger = logger;
            _settingsService = settingsService;
            _entityFactory = entityFactory;
        }

        public async Task<object?> GetViewer(AnalysisContext analysisContext)
        {
            var canOpenContainer = SevenZipService.CanOpenContainer(analysisContext.FilePath);
            if (!canOpenContainer.IsSuccess) 
            { 
                return null; 
            }

            object viewer = new SevenZipUC(analysisContext.FilePath, _messageCenter, _entityFactory);
            return viewer;
        }

    }
}