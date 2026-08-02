using Espluque.Application.Entities;
using Espluque.Application.ModuleManager.Services;
using Espluque.Contracts.Entities;
using Espluque.Contracts.Enums;
using Espluque.Contracts.Interfaces;
using Espluque.Contracts.MessageInterfaces;
using Espluque.Contracts.ModuleInterfaces;
using Espluque.Contracts.ModuleInterfaces.Contributions;
using Espluque.Contracts.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Espluque.Application.Engines
{
    internal class FusionEngine
    {
        private readonly IMessageCenter _messageCenter;
        private readonly Espluque.Contracts.Ports.ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;
        private readonly IThesaurusService _thesaurusService;

        private readonly List<ICatalogEntry> _catalog;

        public event Action<IAnalysisMessage>? AnalyserMessageEvent;

        public FusionEngine(IServiceProvider serviceProvider, List<ICatalogEntry> catalog)
        {
            _messageCenter = serviceProvider.GetRequiredService<IMessageCenter>();
            _logger = serviceProvider.GetRequiredService<Espluque.Contracts.Ports.ILogger>();
            _settingsService = serviceProvider.GetRequiredService<ISettingsService>();
            _entityFactory = serviceProvider.GetRequiredService<IEntityFactory>();
            _thesaurusService = serviceProvider.GetRequiredService<IThesaurusService>();

            _catalog = catalog;
        }

        public async Task<AnalysisContext> FuseAnalysis(AnalysisContext analysisContext)
        {
            string formattedFilename = FormattedFileName(analysisContext);

            List<string>? fusionTags = await _thesaurusService.GetAncestorPreferredTerms(analysisContext.CurrentFileFormat);

            HashSet<string> fusionTagSet = fusionTags?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

            List<ICatalogEntry> fusionerEntries = _catalog
                .Where(entry => entry.InterfaceType == "IFusioner" && entry.Tags.Any(fusionTagSet.Contains))
                .DistinctBy(entry => $"{entry.AssemblyPath}|{entry.ClassName}")
                .ToList();

            foreach (ICatalogEntry entry in fusionerEntries)
            {
                _logger.Log(LogLevel.Debug, $"{formattedFilename}\tFusionEngine: Found fusioner '{entry.ClassName}'");

                (string label, object instance)? contribution = InstanceBuilder.CreateInstance(
                    entry,
                    _messageCenter,
                    _logger,
                    _settingsService,
                    _entityFactory);

                if (contribution is null)
                {
                    _logger.Log(LogLevel.Error, $"{formattedFilename}\tFusionEngine: Cannot create fusioner '{entry.ClassName}'");
                    continue;
                }

                (string label, object instance) = contribution.Value;

                if (instance is not IFusioner fusioner)
                {
                    _logger.Log(LogLevel.Error, $"{formattedFilename}\tFusionEngine: Fusioner '{label}' has invalid instance type: {instance.GetType().FullName}");
                    continue;
                }

                IAssertion assertion = await fusioner.Fuse(analysisContext);

                if (assertion is null)
                {
                    _logger.Log(LogLevel.Warning, $"{formattedFilename}\tFusionEngine: Fusioner '{label}' returned no result");
                    continue;
                }

                analysisContext.Assertions.Add(assertion);
                var messageLabel = assertion.AssertionType ?? "Unknown assertion";
                var messageList = assertion.Summary ?? [];

                IFileInformationPack fileInformationPack = _entityFactory.CreateFileInformationPack(messageLabel, messageList);

                IAnalysisMessage message = new Factory().CreateAnalysisMessage(
                    AnalysisMessageTypeEnum.FusionerSummary,
                    false,
                    null,
                    fileInformationPack,
                    null,
                    null);

                AnalyserMessageEvent?.Invoke(message);

                _logger.Log(LogLevel.Debug, $"{formattedFilename}\tFusionEngine: Fusioner '{label}' done");
            }

            return analysisContext;
        }

        #region Helpers
        private string FormattedFileName(AnalysisContext analysisContext)
        {
            return Path.GetFileName(analysisContext.FilePath).PadRight(35);
        }

        #endregion
    }
}
