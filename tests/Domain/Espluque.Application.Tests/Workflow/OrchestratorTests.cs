using Espluque.Application.Catalog;
using Espluque.Application.Contributions;
using Espluque.Application.CrossCutting;
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
    public class OrchestratorTests
    {
        [Fact]
        public async Task ExecutesAnalysisThenFusion_AndReturnsFinalContext()
        {
            string filePath = Path.GetTempFileName();
            try
            {
                Mock<IThesaurusService> thesaurus = new();
                thesaurus.Setup(x => x.GetConceptMainTermByTerm("Espluque", "AnyFile")).ReturnsAsync((1, "AnyFile"));
                thesaurus.Setup(x => x.GetConceptMainTermByTerm("Test", "PDF")).ReturnsAsync(((int ConceptId, string MainTerm)?)null);
                thesaurus.Setup(x => x.GetAncestorPreferredTerms(It.Is<IFileFormat>(f => f.Label == "PDF"))).ReturnsAsync(["PDF"]);

                List<ICatalogEntry> catalog =
                [
                    CreateEntry(typeof(TestDetector), "IDetector", "AnyFile"),
                    CreateEntry(typeof(TestFusioner), "IFusioner", "PDF")
                ];

                AnalysisContext context = new() { FilePath = filePath };
                IAnalysisContext result = await CreateOrchestrator(thesaurus).ProcessAsync(catalog, context, null);

                Assert.Same(context, result);
                Assert.Equal("PDF", result.CurrentFileFormat!.Label);
                IAssertion assertion = Assert.Single(result.Assertions);
                Assert.Equal("Test assertion", assertion.AssertionType);
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [Fact]
        public async Task RelaysEngineMessages_AndEmitsAnalysisCompleted()
        {
            string filePath = Path.GetTempFileName();
            try
            {
                Mock<IThesaurusService> thesaurus = new();
                thesaurus.Setup(x => x.GetConceptMainTermByTerm("Espluque", "AnyFile"))
                    .ReturnsAsync(((int ConceptId, string MainTerm)?)null);
                thesaurus.Setup(x => x.GetAncestorPreferredTerms(It.IsAny<IFileFormat>()))
                    .ReturnsAsync((List<string>?)null);

                List<ICatalogEntry> catalog = [CreateEntry(typeof(TestGrabber), "IGrabber", "AnyFile")];
                Orchestrator orchestrator = CreateOrchestrator(thesaurus);
                List<IAnalysisMessage> messages = [];
                orchestrator.AnalyserMessageEvent += messages.Add;

                await orchestrator.ProcessAsync(catalog, new AnalysisContext { FilePath = filePath }, null);

                Assert.Equal(2, messages.Count);
                Assert.Equal(AnalysisMessageTypeEnum.GrabberResult, messages[0].MessageType);
                Assert.Equal(AnalysisMessageTypeEnum.AnalysisCompleted, messages[1].MessageType);
                Assert.True(messages[1].IsCompleted);
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        private static Orchestrator CreateOrchestrator(Mock<IThesaurusService> thesaurus) =>
            new(Mock.Of<IMessageCenter>(), Mock.Of<ILogger>(), Mock.Of<ISettingsService>(), new Factory(), thesaurus.Object);

        private static CatalogEntry CreateEntry(Type type, string interfaceType, string tag) => new()
        {
            InterfaceType = interfaceType,
            Label = type.Name,
            ClassName = type.FullName!,
            Tags = [tag],
            AssemblyPath = type.Assembly.Location,
            Assembly = type.Assembly,
            ModuleName = "Test",
            ModuleVersion = "1.0"
        };

        public class TestDetector : IDetector
        {
            public TestDetector(IMessageCenter messageCenter, ILogger logger, ISettingsService settingsService, IEntityFactory entityFactory) { }

            public Task<IFileFormat?> Detect(IAnalysisContext analysisContext) =>
                Task.FromResult<IFileFormat?>(new FileFormat { Referentiel = "Test", Label = "PDF" });
        }

        public class TestFusioner : IFusioner
        {
            public TestFusioner(IMessageCenter messageCenter, ILogger logger, ISettingsService settingsService, IEntityFactory entityFactory) { }

            public Task<IAssertion> Fuse(IAnalysisContext analysisContext) =>
                Task.FromResult<IAssertion>(new Assertion
                {
                    SourceModule = "Test",
                    SourceContribution = "TestFusioner",
                    AssertionType = "Test assertion",
                    ClaimJson = "{}"
                });
        }

        public class TestGrabber : IGrabber
        {
            public TestGrabber(IMessageCenter messageCenter, ILogger logger, ISettingsService settingsService, IEntityFactory entityFactory) { }

            public Task<List<KeyValuePair<string, string>>> Grab(IAnalysisContext analysisContext) =>
                Task.FromResult<List<KeyValuePair<string, string>>>([new("Key", "Value")]);
        }
    }
}