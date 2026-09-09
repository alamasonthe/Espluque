using Espluque.Contracts.Contributions.Types;
using Espluque.Contracts.CrossCutting;
using Espluque.Contracts.Workflow;
using PE.Entities;
using PE.Extensions;
using PE.Services;

namespace PE
{
    public class RsrcManifestGrabber : IGrabber
    {
        private readonly IMessageCenter _messageCenter;
        private readonly Espluque.Contracts.CrossCutting.ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        public RsrcManifestGrabber(
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

        public Task<List<KeyValuePair<string, string>>> Grab(
            IAnalysisContext analysisContext)
        {
            List<KeyValuePair<string, string>> infos = [];

            PeFile peFile = new(
                analysisContext.FilePath ?? string.Empty,
                analysisContext.TempFolderPath,
                _logger);

            PeRsrcSection? rsrcSection =
                peFile.Sections.FirstOrDefault();

            if (rsrcSection?.TypeDirectoryTable?.Entries is not null)
            {
                PeRsrcDirectoryEntry? manifestTypeEntry =
                    rsrcSection.TypeDirectoryTable.Entries
                        .FirstOrDefault(entry =>
                            entry.Name?.Value is not null &&
                            Convert.ToUInt32(entry.Name.Value) == 24);

                if (manifestTypeEntry?.ChildDirectoryTable?.Entries is not null)
                {
                    PeRsrcManifestService service = new(
                        peFile,
                        analysisContext.FilePath ?? string.Empty,
                        _logger);

                    int manifestIndex = 0;

                    foreach (PeRsrcDirectoryEntry nameEntry in
                        manifestTypeEntry.ChildDirectoryTable.Entries)
                    {
                        if (nameEntry.ChildDirectoryTable?.Entries is null)
                            continue;

                        foreach (PeRsrcDirectoryEntry languageEntry in
                            nameEntry.ChildDirectoryTable.Entries)
                        {
                            if (languageEntry.DataEntry is null)
                                continue;

                            string? manifest =
                                service.GetManifest(languageEntry.DataEntry);

                            if (string.IsNullOrWhiteSpace(manifest))
                                continue;

                            string nameId =
                                Convert.ToString(nameEntry.Name?.Value)
                                ?? string.Empty;

                            string languageId =
                                Convert.ToString(languageEntry.Name?.Value)
                                ?? string.Empty;

                            infos.Add(new KeyValuePair<string, string>(
                                $"Manifest[{manifestIndex}] | {nameId} | {languageId}",
                                manifest));

                            manifestIndex++;
                        }
                    }
                }
            }

            peFile.SaveCache(analysisContext.TempFolderPath);

            return Task.FromResult(infos);
        }
    }
}