using Util;
using Espluque.Application.CrossCutting;
using Espluque.Application.Contributions;
using Microsoft.Extensions.Logging;
using Espluque.Application.Catalog;
using Espluque.Contracts.Catalog;
using Espluque.Contracts.Thesaurus;
using Espluque.Contracts.Workflow;
using Espluque.Contracts.CrossCutting;
using Espluque.Contracts.Contributions.Types;
using Espluque.Contracts.Contributions;

namespace Espluque.Application.Workflow
{
    /// <summary>
    /// Coordinates the complete file analysis process by executing viewer, grabber, and detector contributions
    /// and by maintaining the analysis context throughout the detection workflow.
    /// </summary>
    /// <remarks>
    /// The analysis starts from the tag defined in AnalysisContext.StartingTag or Default (AnyFile)
    /// The detected format is refined as detector contributions are executed.
    /// 
    /// Processing cycle:
    /// <code>
    /// AnalysisContext
    ///     ↓
    /// Initialize the starting tag
    ///     ↓
    /// Initialize viewer and grabber backlogs with the starting tag
    ///     ↓
    /// Execute viewers
    ///     ↓
    /// Execute grabbers and collect observed information
    ///     ↓
    /// Select and execute detectors from the current thesaurus concept (Tag)
    ///     ↓
    /// Resolve the most specific detected file format
    ///     ↓
    /// Add ancestor concepts to viewer and grabber backlogs
    ///     ↓
    /// Repeat until no contribution remains to execute
    /// </code>
    /// 
    /// Detector selection is driven by the thesaurus:
    /// - detectors matching the current concept are selected first;
    /// - when none are available, descendant concepts are explored by increasing distance;
    /// - each detector contribution is executed once during an analysis.
    /// 
    /// The engine updates the supplied AnalysisContext with the starting tag, current file format,
    /// format history, tag history, and information collected by grabber contributions.
    /// 
    /// Analysis messages are emitted through AnalyserMessageEvent to expose viewer contributions
    /// and grabber results to the presentation layer.
    /// </remarks>

    public class AnalysisEngine
    {
        private readonly IMessageCenter _messageCenter;
        private readonly Contracts.CrossCutting.ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;
        private readonly IThesaurusService _thesaurusService;

        private readonly List<ICatalogEntry> _catalog;

        public event Action<IAnalysisMessage>? AnalyserMessageEvent;

        private List<TaskRequest> _viewerBacklog;
        private List<TaskRequest> _grabberBacklog;
        private List<string> _doneDetectors = [];

        private IAnalysisContext _analysisContext;

        public AnalysisEngine(
            IMessageCenter messageCenter,
            Contracts.CrossCutting.ILogger logger,
            ISettingsService settingsService,
            IEntityFactory entityFactory,
            IThesaurusService thesaurusService,
            List<ICatalogEntry> catalog)
        {
            _messageCenter = messageCenter;
            _logger = logger;
            _settingsService = settingsService;
            _entityFactory = entityFactory;
            _thesaurusService = thesaurusService;
            _catalog = catalog;
        }

        private void InitializeAnalyze(IAnalysisContext analysisContext)
        {
            _analysisContext = analysisContext;

            _analysisContext.StartingTag = string.IsNullOrWhiteSpace(_analysisContext.StartingTag)
                ? "AnyFile"
                : _analysisContext.StartingTag;

            _viewerBacklog = new() { new TaskRequest { Tag = _analysisContext.StartingTag } };

            _grabberBacklog = new() { new TaskRequest { Tag = _analysisContext.StartingTag } };

            _analysisContext.CurrentFileFormat = _entityFactory.CreateFileFormat( "Espluque", _analysisContext.StartingTag, null, null);
        }

        public async Task<IAnalysisContext> AnalyzeFileAsync(IAnalysisContext analysisContext, string? viewerType = null)
        {
            InitializeAnalyze(analysisContext);

            _logger.Log(LogLevel.Information, $"{FormattedFileName()}\t--------------- Analysis started --------------- : {_analysisContext.FilePath}");

            Result<bool> canOpenReadResult = Util.File.CanOpenRead(_analysisContext.FilePath);

            if (!canOpenReadResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"{FormattedFileName()}\tFile check failed: {canOpenReadResult.Error?.Code} - {canOpenReadResult.Error?.Message}");
                _logger.Log(LogLevel.Information, $"{FormattedFileName()}\t--------------- Analysis finished --------------- ");
                return _analysisContext;
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

            // AnalyserMessageEvent?.Invoke(new Factory().CreateAnalysisMessage(AnalysisMessageTypeEnum.AnalysisCompleted, true, null, null, null, null));

            _logger.Log( LogLevel.Information, $"{FormattedFileName()}\tCurrent format: Referentiel={_analysisContext.CurrentFileFormat.Referentiel}, Label={_analysisContext.CurrentFileFormat.Label}, Version={_analysisContext.CurrentFileFormat.Version}, MIMEType={_analysisContext.CurrentFileFormat.MIMEType}");
            _logger.Log(LogLevel.Information, $"{FormattedFileName()}\t--------------- Analysis finished --------------- ");

            return _analysisContext;
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

                    List<KeyValuePair<string, string>> keyValueList = await grabber.Grab(_analysisContext);

                    GrabberResult grabberResult = new()
                    {
                        ModuleName = grabberEntry.ModuleName,
                        ContributionLabel = label,
                        GrabbedInformation = keyValueList
                    };
                    _analysisContext.ObservedData.Add(grabberResult);

                    IFileInformationPack fileInformationPack = _entityFactory.CreateFileInformationPack(label, keyValueList);

                    IAnalysisMessage message = new Factory().CreateAnalysisMessage(AnalysisMessageTypeEnum.GrabberResult, false, null, fileInformationPack, null, null);

                    AnalyserMessageEvent?.Invoke(message);
                }

                grabberTaskRequest.Status = TaskStatusEnum.Done;
                _logger.Log(LogLevel.Debug, $"{FormattedFileName()}\tTask grabber {grabberTaskRequest.Tag} done");
            }
        }

        private async Task<List<ICatalogEntry>> GetDetectorsToExecuteAsync()
        {
            List<ICatalogEntry> detectorsToExecute = [];

            (int conceptId, string mainTerm)? conceptMainTerm = await _thesaurusService.GetConceptMainTermByTerm(_analysisContext.CurrentFileFormat.Referentiel!, _analysisContext.CurrentFileFormat.Label!);
            if (conceptMainTerm is null && !string.IsNullOrWhiteSpace(_analysisContext.CurrentFileFormat.MIMEType))
            {
                conceptMainTerm = await _thesaurusService.GetConceptMainTermByTerm("MIMEType", _analysisContext.CurrentFileFormat.MIMEType);
            }
            if (conceptMainTerm is null) return detectorsToExecute;

            _analysisContext.TagHistory.Add(conceptMainTerm.Value.mainTerm);
            _logger.Log(LogLevel.Debug, $"Detector search: {_analysisContext.CurrentFileFormat.Referentiel} / {_analysisContext.CurrentFileFormat.Label} => thesaurus tag \"{conceptMainTerm.Value.mainTerm}\"");

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

                IFileFormat? fileFormat = await detector.Detect(_analysisContext);

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
                    _analysisContext.FileFormatHistory.Add(_analysisContext.CurrentFileFormat);
                    _analysisContext.CurrentFileFormat = detectedFileFormats[0];
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
                _analysisContext.FileFormatHistory.Add(format);
            }

            _analysisContext.CurrentFileFormat = detectedFileFormats[0];
        }

        private async Task<bool> IsSecondFormatMoreSpecificAsync( IFileFormat firstFormat, IFileFormat secondFormat)
        {
            (int ConceptId, string MainTerm)? firstConcept = await FindConceptAsync(firstFormat);
            (int ConceptId, string MainTerm)? secondConcept = await FindConceptAsync(secondFormat);

            switch (firstConcept, secondConcept)
            {
                case (null, _):
                    _logger.Log(LogLevel.Warning, $"Format is missing in thesaurus: {firstFormat.Referentiel} - {firstFormat.Label}");
                    return false;
                case (_, null):
                    _logger.Log( LogLevel.Warning, $"Format is missing in thesaurus: {secondFormat.Referentiel} - {secondFormat.Label}");
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

        private async Task<(int ConceptId, string MainTerm)?> FindConceptAsync(IFileFormat fileFormat)
        {
            (int ConceptId, string MainTerm)? concept = null;

            if (!string.IsNullOrWhiteSpace(fileFormat.Referentiel) && !string.IsNullOrWhiteSpace(fileFormat.Label))
            {
                concept = await _thesaurusService.GetConceptMainTermByTerm(fileFormat.Referentiel, fileFormat.Label);
            }

            if (concept is null && !string.IsNullOrWhiteSpace(fileFormat.MIMEType))
            {
                concept = await _thesaurusService.GetConceptMainTermByTerm("MIMEType", fileFormat.MIMEType);
            }

            return concept;
        }

        private async Task UpdateBacklogsAsync()
        {
            List<string>? taskTags = await _thesaurusService.GetAncestorPreferredTerms(_analysisContext.CurrentFileFormat);

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
            return Path.GetFileName(_analysisContext.FilePath).PadRight(35);
        }

        #endregion
    }
}
