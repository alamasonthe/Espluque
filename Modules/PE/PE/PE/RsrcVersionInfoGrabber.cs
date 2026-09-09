using Espluque.Contracts.Contributions.Types;
using Espluque.Contracts.CrossCutting;
using Espluque.Contracts.Workflow;
using PE.Entities;
using PE.Extensions;
using PE.Services;

namespace PE
{
    public class RsrcVersionInfoGrabber : IGrabber
    {
        private readonly IMessageCenter _messageCenter;
        private readonly Espluque.Contracts.CrossCutting.ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        public RsrcVersionInfoGrabber(
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
            List<KeyValuePair<string, string>> infos = [];

            PeFile peFile = new(
                analysisContext.FilePath ?? string.Empty,
                analysisContext.TempFolderPath,
                _logger);

            PeRsrcSection? rsrcSection = peFile.Sections.FirstOrDefault();

            if (rsrcSection?.TypeDirectoryTable?.Entries is not null)
            {
                PeRsrcDirectoryEntry? versionTypeEntry =
                    rsrcSection.TypeDirectoryTable.Entries
                        .FirstOrDefault(entry =>
                            entry.Name?.Value is not null &&
                            Convert.ToUInt32(entry.Name.Value) == 16);

                if (versionTypeEntry?.ChildDirectoryTable?.Entries is not null)
                {
                    PeRsrcVersionInfoService service = new(
                        peFile,
                        analysisContext.FilePath ?? string.Empty,
                        _logger);

                    foreach (PeRsrcDirectoryEntry nameEntry in
                        versionTypeEntry.ChildDirectoryTable.Entries)
                    {
                        if (nameEntry.ChildDirectoryTable?.Entries is null)
                            continue;

                        foreach (PeRsrcDirectoryEntry languageEntry in
                            nameEntry.ChildDirectoryTable.Entries)
                        {
                            if (languageEntry.DataEntry is null)
                                continue;

                            infos.AddRange(
                                service.GetVersionInfo(languageEntry.DataEntry));
                        }
                    }
                }
            }

            peFile.SaveCache(analysisContext.TempFolderPath);

            return Task.FromResult(infos);
        }
    }
}