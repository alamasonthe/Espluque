using Espluque.Contracts.Contributions.Types;
using Espluque.Contracts.CrossCutting;
using Espluque.Contracts.Workflow;

namespace WindowsInstaller
{
    public class MsiSummaryInfos: IGrabber
    {
        private readonly IMessageCenter _messageCenter;
        private readonly ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        public MsiSummaryInfos()
        {

        }

        public MsiSummaryInfos(IMessageCenter messageCenter,
            ILogger logger,
            ISettingsService settingsService,
            IEntityFactory entityFactory)
        {
            _messageCenter = messageCenter;
            _logger = logger;
            _settingsService = settingsService;
            _entityFactory = entityFactory;
        }

        public async Task<List<KeyValuePair<string, string>>> Grab(IAnalysisContext analysisContext)
        {
            try
            {
                WindowsInstallerService windowsInstallerService = new WindowsInstallerService();
                return windowsInstallerService.GetSummaryInfos(analysisContext.FilePath);
            }
            catch (Exception ex)
            {
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"Windows Installer module: {ex.Message}");
                return new List<KeyValuePair<string, string>>();
            }
        }
    }
}
