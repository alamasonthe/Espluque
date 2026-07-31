using Util;
using Espluque.Application.Entities;
using Espluque.Application.DetectionResult;
using Espluque.Contracts.Enums;
using Espluque.Contracts.Interfaces;
using Espluque.Contracts.MessageInterfaces;
using Espluque.Contracts.ModuleInterfaces;
using Espluque.Contracts.Ports;
using Espluque.Contracts.DetectionResult;
using Espluque.Application.ModuleManager.Services;
using Espluque.Contracts.Detection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Espluque.Contracts.ModuleInterfaces.Contributions;
using Espluque.Contracts.Entities;

namespace Espluque.Application.Detection
{
    public class Engine: IEngine
    {
        private readonly IMessageCenter _messageCenter;
        private readonly Espluque.Contracts.Ports.ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;
        private readonly IThesaurusService _thesaurusService;

        private readonly List<ICatalogEntry> _catalog;

        public event Action<IAnalysisMessage>? AnalyserMessageEvent;

        private List<TaskRequest> _viewerBacklog;
        private List<TaskRequest> _grabberBacklog;
        private List<string> _doneDetectors = [];

        private IEngineResult _engineResult = new EngineResult();

        public Engine(IServiceProvider serviceProvider, List<ICatalogEntry> catalog)
        {
            _messageCenter = serviceProvider.GetRequiredService<IMessageCenter>();
            _logger = serviceProvider.GetRequiredService<Espluque.Contracts.Ports.ILogger>();
            _settingsService = serviceProvider.GetRequiredService<ISettingsService>();
            _entityFactory = serviceProvider.GetRequiredService<IEntityFactory>();
            _thesaurusService = serviceProvider.GetRequiredService<IThesaurusService>();

            _catalog = catalog;
        }

        private void InitializeAnalyze(AnalysisContext analysisContext)
        {
            _engineResult.AnalysisContext = analysisContext;

            _viewerBacklog = new()
            {
                new TaskRequest { Tag = "AnyFile" }
            };

            _grabberBacklog = new()
            {
                new TaskRequest { Tag = "AnyFile" }
            };

            _engineResult.AnalysisContext.CurrentFileFormat = _entityFactory.CreateFileFormat("Espluque", "AnyFile", null, null);
        }

        public async Task<IEngineResult> AnalyzeFileAsync(AnalysisContext analysisContext, string? viewerType = null)
        {
            InitializeAnalyze(analysisContext);

            _logger.Log(LogLevel.Information, $"{FormattedFileName()}\t--------------- Analysis started --------------- : {_engineResult.AnalysisContext.FilePath}");

            Result<bool> canOpenReadResult = Util.File.CanOpenRead(_engineResult.AnalysisContext.FilePath);

            if (!canOpenReadResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"{FormattedFileName()}\tFile check failed: {canOpenReadResult.Error?.Code} - {canOpenReadResult.Error?.Message}");
                _logger.Log(LogLevel.Information, $"{FormattedFileName()}\t--------------- Analysis finished --------------- ");
                return _engineResult;
            }

            var detectorsToExecute = await GetDetectorsToExecuteAsync();

            while (_viewerBacklog.Any(taskRequest => taskRequest.Status == TaskStatusEnum.ToDo)
                | _grabberBacklog.Any(taskRequest => taskRequest.Status == TaskStatusEnum.ToDo)
                | detectorsToExecute.Count > 0)
            {

                await ExecuteViewerTaskAsync(viewerType);
                await ExecuteGrabberTaskAsync();
                List<IFileFormat> detectedFileFormats =  await ExecuteDetectorsAsync(detectorsToExecute);
                await UpdateCurrentFormatAsync(detectedFileFormats);
                await UpdateBacklogsAsync();
                detectorsToExecute = await GetDetectorsToExecuteAsync();
            }

            AnalyserMessageEvent?.Invoke(new Factory().CreateAnalysisMessage(AnalysisMessageTypeEnum.AnalysisCompleted, true, null, null, null, null));

            _logger.Log( LogLevel.Information, $"{FormattedFileName()}\tCurrent format: Referentiel={_engineResult.AnalysisContext.CurrentFileFormat.Referentiel}, Label={_engineResult.AnalysisContext.CurrentFileFormat.Label}, Version={_engineResult.AnalysisContext.CurrentFileFormat.Version}, MIMEType={_engineResult.AnalysisContext.CurrentFileFormat.MIMEType}");
            _logger.Log(LogLevel.Information, $"{FormattedFileName()}\t--------------- Analysis finished --------------- ");

            return _engineResult;
        }

        private async Task ExecuteViewerTaskAsync(string? viewerType = null)
        {
            TaskRequest? viewerTaskRequest = _viewerBacklog.FirstOrDefault(x => x.Status == TaskStatusEnum.ToDo);
            if (viewerTaskRequest is not null)
            {
                _logger.Log(LogLevel.Debug, $"{FormattedFileName()}\tCatalog viewers for {viewerTaskRequest.Tag}: {string.Join(", ", _catalog.Where(entry => entry.InterfaceType == viewerType && entry.Tags.Any(tag => string.Equals(tag, viewerTaskRequest.Tag, StringComparison.OrdinalIgnoreCase))).Select(entry => $"{entry.Label} [{entry.ClassName}]"))}");
                await foreach ((string label, object instance) in InstanceBuilder.CreateInstancesAsync(_catalog, viewerType, viewerTaskRequest!.Tag, _messageCenter, _logger, _settingsService, _entityFactory))
                {
                    if (string.IsNullOrEmpty(label) || instance is null)
                    {
                        _logger.Log(LogLevel.Warning, $"{FormattedFileName()}\tTask viewer cannot create instance for {viewerTaskRequest!.Tag}");
                    }
                    else
                    {
                        var message = new Factory().CreateAnalysisMessage(AnalysisMessageTypeEnum.ViewerUC, false, null, null, label, instance);
                        _logger.Log(LogLevel.Debug, $"{FormattedFileName()}\tTask viewer {viewerTaskRequest.Tag}: sending message for {label}");
                        AnalyserMessageEvent?.Invoke(message);
                    }
                }
                viewerTaskRequest.Status = TaskStatusEnum.Done;
                _logger.Log(LogLevel.Debug, $"{FormattedFileName()}\tTask viewer {viewerTaskRequest.Tag} queued");
            }
        }

        private async Task ExecuteGrabberTaskAsync()
        {
            TaskRequest? grabberTaskRequest = _grabberBacklog.FirstOrDefault(x => x.Status == TaskStatusEnum.ToDo);
            if (grabberTaskRequest is not null)
            {
                List<ICatalogEntry> grabberEntries = CatalogService.FilterCatalog(_catalog, "IGrabber", grabberTaskRequest.Tag);

                foreach (ICatalogEntry grabberEntry in grabberEntries)
                {
                    (string label, object instance)? contribution = InstanceBuilder.CreateInstance(
                        grabberEntry,
                        _messageCenter,
                        _logger,
                        _settingsService,
                        _entityFactory);

                    if (contribution is null)
                    {
                        continue;
                    }

                    (string label, object instance) = contribution.Value;

                    if (instance is not IGrabber grabber)
                    {
                        _logger.Log(LogLevel.Error, $"{FormattedFileName()}\tTask grabber {label} invalid instance type: {instance.GetType().FullName}");
                        continue;
                    }

                    List<KeyValuePair<string, string>> keyValueList = await grabber.Grab(_engineResult.AnalysisContext);

                    GrabberResult grabberResult = new()
                    {
                        ModuleName = grabberEntry.ModuleName,
                        ContributionLabel = label,
                        GrabbedInformation = keyValueList
                    };
                    _engineResult.GrabberResults.Add(grabberResult);

                    IFileInformationPack fileInformationPack = _entityFactory.CreateFileInformationPack(label, keyValueList);

                    IAnalysisMessage message = new Factory().CreateAnalysisMessage(AnalysisMessageTypeEnum.GrabberResult, false, null, fileInformationPack, label, instance);

                    AnalyserMessageEvent?.Invoke(message);
                }

                grabberTaskRequest.Status = TaskStatusEnum.Done;
                _logger.Log(LogLevel.Debug, $"{FormattedFileName()}\tTask grabber {grabberTaskRequest.Tag} done");
            }
        }

        private async Task<List<ICatalogEntry>> GetDetectorsToExecuteAsync()
        {
            List<ICatalogEntry> detectorsToExecute = [];

            (int conceptId, string mainTerm)? conceptMainTerm = await _thesaurusService.GetConceptMainTermByTerm(_engineResult.AnalysisContext.CurrentFileFormat.Referentiel!, _engineResult.AnalysisContext.CurrentFileFormat.Label!);
            if (conceptMainTerm is null && !string.IsNullOrWhiteSpace(_engineResult.AnalysisContext.CurrentFileFormat.MIMEType))
            {
                conceptMainTerm = await _thesaurusService.GetConceptMainTermByTerm("MIMEType", _engineResult.AnalysisContext.CurrentFileFormat.MIMEType);
            }
            if (conceptMainTerm is null) return detectorsToExecute;

            _logger.Log(LogLevel.Debug, $"Detector search: {_engineResult.AnalysisContext.CurrentFileFormat.Referentiel} / {_engineResult.AnalysisContext.CurrentFileFormat.Label} => thesaurus tag \"{conceptMainTerm.Value.mainTerm}\"");

            detectorsToExecute = _catalog
                .Where(entry =>
                    entry.InterfaceType == "IDetector"
                    && entry.Tags.Any(tag => string.Equals(tag, conceptMainTerm.Value.mainTerm, StringComparison.OrdinalIgnoreCase))
                    && !_doneDetectors.Contains($"{entry.AssemblyPath}|{entry.ClassName}"))
                .ToList();

            if (detectorsToExecute.Count == 0)
            {
                var descendantLinks = await _thesaurusService.GetDescendantLinks(conceptMainTerm.Value.conceptId);
                if (descendantLinks is null) return detectorsToExecute;

                List<(int ConceptId, int Distance)> descendantsByDistance = GetDescendantsByDistance(conceptMainTerm.Value.conceptId, descendantLinks);

                List<(int ConceptId, string MainTerm)>? descendantRefs = await _thesaurusService.GetDescendantRefs(conceptMainTerm.Value.conceptId);
                if (descendantRefs is null) return detectorsToExecute;

                List<(int ConceptId, string MainTerm, int Distance)> descendants = descendantsByDistance
                    .Join(
                        descendantRefs,
                        descendantByDistance => descendantByDistance.ConceptId,
                        descendantRef => descendantRef.ConceptId,
                        (descendantByDistance, descendantRef) => (
                            ConceptId: descendantRef.ConceptId,
                            MainTerm: descendantRef.MainTerm,
                            Distance: descendantByDistance.Distance))
                    .ToList();

                foreach (var level in descendants.GroupBy(x => x.Distance).OrderBy(x => x.Key))
                {
                    foreach (var concept in level)
                    {
                        List<ICatalogEntry> matchingDetectors = _catalog
                            .Where(entry =>
                                entry.InterfaceType == "IDetector"
                                && entry.Tags.Any(tag => string.Equals(tag, concept.MainTerm, StringComparison.OrdinalIgnoreCase))
                                && !_doneDetectors.Contains($"{entry.AssemblyPath}|{entry.ClassName}"))
                            .ToList();

                        detectorsToExecute.AddRange(matchingDetectors);

                        detectorsToExecute = detectorsToExecute .DistinctBy(entry => $"{entry.AssemblyPath}|{entry.ClassName}").ToList();
                    }

                    if (detectorsToExecute.Count > 0)
                    {
                        return detectorsToExecute;
                    }
                }
            }
            return detectorsToExecute;
        }

        private async Task<List<IFileFormat>> ExecuteDetectorsAsync( List<ICatalogEntry> detectorsToExecute)
        {
            List<IFileFormat> detectedFileFormats = [];

            foreach (ICatalogEntry detectorEntry in detectorsToExecute)
            {
                (string label, object instance)? contribution = InstanceBuilder.CreateInstance(
                    detectorEntry,
                    _messageCenter,
                    _logger,
                    _settingsService,
                    _entityFactory);

                if (contribution is null)
                {
                    _doneDetectors.Add($"{detectorEntry.AssemblyPath}|{detectorEntry.ClassName}");
                    _logger.Log( LogLevel.Error, $"{FormattedFileName()}\tTask detector {detectorEntry.Label} failed: Cannot create instance");
                    continue;
                }

                (string label, object instance) = contribution.Value;

                IDetector detector = (IDetector)instance;

                IFileFormat? fileFormat = await detector.Detect(_engineResult.AnalysisContext);

                if (fileFormat is null)
                {
                    _logger.Log( LogLevel.Warning, $"{FormattedFileName()}\tTask detector {label} returned no format");

                    _doneDetectors.Add($"{detectorEntry.AssemblyPath}|{detectorEntry.ClassName}");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(fileFormat.Label)
                    || !string.IsNullOrWhiteSpace(fileFormat.MIMEType))
                {
                    detectedFileFormats.Add(fileFormat);
                }

                _doneDetectors.Add($"{detectorEntry.AssemblyPath}|{detectorEntry.ClassName}");
                _logger.Log(LogLevel.Debug, $"{FormattedFileName()}\tTask detector {label} done");
            }

            return detectedFileFormats;
        }

        private async Task UpdateCurrentFormatAsync(List<IFileFormat> detectedFileFormats)
        {
            switch (detectedFileFormats.Count)
            {
                case 0:
                    return;

                case 1:
                    _engineResult.AnalysisContext.FileFormatHistory.Add(_engineResult.AnalysisContext.CurrentFileFormat);
                    _engineResult.AnalysisContext.CurrentFileFormat = detectedFileFormats[0];
                    return;
            }

            bool hasMoved;
            do
            {
                hasMoved = false;

                for (int index = 0; index < detectedFileFormats.Count - 1; index++)
                {
                    IFileFormat firstFormat = detectedFileFormats[index];
                    IFileFormat secondFormat = detectedFileFormats[index + 1];

                    bool shouldSwap = await IsSecondFormatMoreSpecificAsync(firstFormat, secondFormat);

                    if (shouldSwap)
                    {
                        detectedFileFormats[index] = secondFormat;
                        detectedFileFormats[index + 1] = firstFormat;
                        hasMoved = true;
                    }
                }
            }
            while (hasMoved);

            foreach (IFileFormat format in detectedFileFormats)
            {
                _engineResult.AnalysisContext.FileFormatHistory.Add(format);
            }

            _engineResult.AnalysisContext.CurrentFileFormat = detectedFileFormats[0];
        }

        private async Task<bool> IsSecondFormatMoreSpecificAsync( IFileFormat firstFormat, IFileFormat secondFormat)
        {
            string firstReferenceName = !string.IsNullOrWhiteSpace(firstFormat.MIMEType) ? "MIMEType" : firstFormat.Referentiel!;
            string firstTerm = !string.IsNullOrWhiteSpace(firstFormat.MIMEType) ? firstFormat.MIMEType! : firstFormat.Label!;
            (int ConceptId, string MainTerm)? firstConcept = await _thesaurusService.GetConceptMainTermByTerm( firstReferenceName, firstTerm);

            string secondReferenceName = !string.IsNullOrWhiteSpace(secondFormat.MIMEType) ? "MIMEType" : secondFormat.Referentiel!;
            string secondTerm = !string.IsNullOrWhiteSpace(secondFormat.MIMEType) ? secondFormat.MIMEType! : secondFormat.Label!;
            (int ConceptId, string MainTerm)? secondConcept = await _thesaurusService.GetConceptMainTermByTerm( secondReferenceName, secondTerm);

            switch (firstConcept, secondConcept)
            {
                case (null, _):
                    _logger.Log( LogLevel.Warning, $"Format is missing in thesaurus: {firstReferenceName} - {firstTerm}");
                    return false;
                case (_, null):
                    _logger.Log( LogLevel.Warning, $"Format is missing in thesaurus: {secondReferenceName} - {secondTerm}");
                    return false;
            }

            if (firstConcept.Value.ConceptId == secondConcept.Value.ConceptId)
            {
                return false;
            }

            bool? secondIsDescendant = await _thesaurusService.GetConceptPathExists(firstConcept.Value.ConceptId,secondConcept.Value.ConceptId);
            switch (secondIsDescendant)
            {
                case null:
                    return false;
                case true:
                    return true;
            }

            bool? firstIsDescendant = await _thesaurusService.GetConceptPathExists( secondConcept.Value.ConceptId, firstConcept.Value.ConceptId);
            switch (firstIsDescendant)
            {
                case null:
                case true:
                    return false;
            }

            // Hybrid format!!
            return false;
        }

        private async Task UpdateBacklogsAsync()
        {
            List<string>? taskTags = await _thesaurusService.GetAncestorPreferredTerms(_engineResult.AnalysisContext.CurrentFileFormat);

            if (taskTags is null)
            {
                return;
            }

            foreach (string tag in taskTags)
            {
                if (string.IsNullOrWhiteSpace(tag))
                {
                    continue;
                }

                if (!_viewerBacklog.Any(taskRequest =>
                    string.Equals(taskRequest.Tag, tag, StringComparison.OrdinalIgnoreCase)))
                {
                    _viewerBacklog.Add(new TaskRequest
                    {
                        Tag = tag
                    });
                }

                if (!_grabberBacklog.Any(taskRequest =>
                    string.Equals(taskRequest.Tag, tag, StringComparison.OrdinalIgnoreCase)))
                {
                    _grabberBacklog.Add(new TaskRequest
                    {
                        Tag = tag
                    });

                    _logger.Log(LogLevel.Debug, $"{FormattedFileName()}\tTask grabber {tag}: ToDo");
                }
            }
        }

        #region Helpers

        private static List<(int ConceptId, int Distance)> GetDescendantsByDistance( int conceptId, List<(int ParentConceptId, int ChildConceptId)> descendantLinks)
        {
            List<(int ConceptId, int Distance)> descendants = [];
            List<int> visitedConceptIds = [conceptId];
            Queue<(int ConceptId, int Distance)> conceptsToVisit  = new();

            conceptsToVisit .Enqueue((conceptId, 0));

            while (conceptsToVisit .Count > 0)
            {
                (int currentConceptId, int currentDistance) = conceptsToVisit .Dequeue();

                foreach (var link in descendantLinks.Where(x => x.ParentConceptId == currentConceptId))
                {
                    if (visitedConceptIds.Contains(link.ChildConceptId))
                    {
                        continue;
                    }

                    visitedConceptIds.Add(link.ChildConceptId);
                    descendants.Add((link.ChildConceptId, currentDistance + 1));
                    conceptsToVisit .Enqueue((link.ChildConceptId, currentDistance + 1));
                }
            }

            return descendants;
        }

        private string FormattedFileName()
        {
            return Path.GetFileName(_engineResult.AnalysisContext.FilePath).PadRight(35);
        }

        #endregion
    }
}
