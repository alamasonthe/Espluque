using Espluque.Application.Catalog;
using Espluque.Application.Contributions;
using Espluque.Application.Workflow;
using Espluque.Contracts.Catalog;
using Espluque.Contracts.Contributions;
using Espluque.Contracts.Contributions.Types;
using Espluque.Contracts.CrossCutting;
using Espluque.Contracts.Thesaurus;
using Espluque.Contracts.Workflow;
using Moq;

namespace Espluque.Application.Tests.Workflow
{
    public class AnalysisEngine_GetDetectorsToExecuteAsync
    {
        private static readonly List<string> ExecutionOrder = [];

        [Fact]
        public async Task SelectsDetectorMatchingCurrentConcept()
        {
            MatchingDetector.CallCount = 0;
            OtherDetector.CallCount = 0;

            Mock<IThesaurusService> thesaurus = CreateThesaurus();
            List<ICatalogEntry> catalog =
            [
                CreateCatalogEntry(typeof(MatchingDetector), "AnyFile"),
                CreateCatalogEntry(typeof(OtherDetector), "PDF")
            ];

            await ExecuteAnalysis(CreateEngine(thesaurus, catalog));

            Assert.Equal(1, MatchingDetector.CallCount);
            Assert.Equal(0, OtherDetector.CallCount);
        }

        [Fact]
        public async Task UsesNearestDescendantDetector_WhenCurrentConceptHasNoDetector()
        {
            ExecutionOrder.Clear();

            Mock<IThesaurusService> thesaurus = CreateThesaurus();
            List<(int ParentConceptId, int ChildConceptId)> links = [(1, 2), (2, 3)];
            List<(int ConceptId, string MainTerm)> refs = [(2, "Near"), (3, "Far")];

            thesaurus.Setup(x => x.GetDescendantLinks(1)).ReturnsAsync(links);
            thesaurus.Setup(x => x.GetDescendantRefs(1)).ReturnsAsync(refs);

            List<ICatalogEntry> catalog =
            [
                CreateCatalogEntry(typeof(NearDetector), "Near"),
                CreateCatalogEntry(typeof(FarDetector), "Far")
            ];

            await ExecuteAnalysis(CreateEngine(thesaurus, catalog));

            Assert.Equal(["Near", "Far"], ExecutionOrder);
        }

        [Fact]
        public async Task ExecutesEachDetectorOnlyOnce_DuringAnalysis()
        {
            OnceDetector.CallCount = 0;

            Mock<IThesaurusService> thesaurus = CreateThesaurus();
            List<ICatalogEntry> catalog = [CreateCatalogEntry(typeof(OnceDetector), "AnyFile")];

            await ExecuteAnalysis(CreateEngine(thesaurus, catalog));

            Assert.Equal(1, OnceDetector.CallCount);
        }

        private static Mock<IThesaurusService> CreateThesaurus()
        {
            Mock<IThesaurusService> thesaurus = new();
            (int ConceptId, string MainTerm)? concept = (1, "AnyFile");

            thesaurus.Setup(x => x.GetConceptMainTermByTerm("Espluque", "AnyFile")).ReturnsAsync(concept);
            thesaurus.Setup(x => x.GetAncestorPreferredTerms(It.IsAny<IFileFormat>())).ReturnsAsync((List<string>?)null);
            thesaurus.Setup(x => x.GetDescendantLinks(1)).ReturnsAsync([]);
            thesaurus.Setup(x => x.GetDescendantRefs(1)).ReturnsAsync([]);

            return thesaurus;
        }

        private static AnalysisEngine CreateEngine(Mock<IThesaurusService> thesaurus, List<ICatalogEntry> catalog)
        {
            Mock<IEntityFactory> entityFactory = new();
            entityFactory.Setup(x => x.CreateFileFormat("Espluque", "AnyFile", null, null))
                .Returns(new FileFormat { Referentiel = "Espluque", Label = "AnyFile" });

            return new AnalysisEngine(
                Mock.Of<IMessageCenter>(),
                Mock.Of<ILogger>(),
                Mock.Of<ISettingsService>(),
                entityFactory.Object,
                thesaurus.Object,
                catalog);
        }

        private static async Task ExecuteAnalysis(AnalysisEngine engine)
        {
            string filePath = Path.GetTempFileName();
            try
            {
                await engine.AnalyzeFileAsync(new AnalysisContext { FilePath = filePath }, "IWpfViewer");
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        private static CatalogEntry CreateCatalogEntry(Type type, string tag) => new()
        {
            InterfaceType = "IDetector",
            Label = type.Name,
            ClassName = type.FullName!,
            Tags = [tag],
            AssemblyPath = type.Assembly.Location,
            Assembly = type.Assembly,
            ModuleName = "Test",
            ModuleVersion = "1.0"
        };

        public class MatchingDetector : IDetector
        {
            public static int CallCount { get; set; }
            public MatchingDetector(IMessageCenter messageCenter, ILogger logger, ISettingsService settingsService, IEntityFactory entityFactory) { }

            public Task<IFileFormat?> Detect(IAnalysisContext analysisContext)
            {
                CallCount++;
                return Task.FromResult<IFileFormat?>(null);
            }
        }

        public class OtherDetector : IDetector
        {
            public static int CallCount { get; set; }
            public OtherDetector(IMessageCenter messageCenter, ILogger logger, ISettingsService settingsService, IEntityFactory entityFactory) { }

            public Task<IFileFormat?> Detect(IAnalysisContext analysisContext)
            {
                CallCount++;
                return Task.FromResult<IFileFormat?>(null);
            }
        }

        public class NearDetector : IDetector
        {
            public NearDetector(IMessageCenter messageCenter, ILogger logger, ISettingsService settingsService, IEntityFactory entityFactory) { }

            public Task<IFileFormat?> Detect(IAnalysisContext analysisContext)
            {
                ExecutionOrder.Add("Near");
                return Task.FromResult<IFileFormat?>(null);
            }
        }

        public class FarDetector : IDetector
        {
            public FarDetector(IMessageCenter messageCenter, ILogger logger, ISettingsService settingsService, IEntityFactory entityFactory) { }

            public Task<IFileFormat?> Detect(IAnalysisContext analysisContext)
            {
                ExecutionOrder.Add("Far");
                return Task.FromResult<IFileFormat?>(null);
            }
        }

        public class OnceDetector : IDetector
        {
            public static int CallCount { get; set; }
            public OnceDetector(IMessageCenter messageCenter, ILogger logger, ISettingsService settingsService, IEntityFactory entityFactory) { }

            public Task<IFileFormat?> Detect(IAnalysisContext analysisContext)
            {
                CallCount++;
                return Task.FromResult<IFileFormat?>(new FileFormat { Referentiel = "Espluque", Label = "AnyFile" });
            }
        }
    }
}