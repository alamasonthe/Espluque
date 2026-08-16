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
    public class FusionEngineTests
    {
        [Fact]
        public async Task ExecutesMatchingFusioners_AndAddsAssertions()
        {
            TestFusioner.Reset();
            TestFusioner.Result = CreateAssertion();

            var thesaurus = CreateThesaurus("PDF");
            var context = CreateContext();

            FusionEngine engine = CreateEngine(
                thesaurus,
                [CreateCatalogEntry("PDF")]);

            await engine.FuseAnalysis(context);

            Assert.Equal(1, TestFusioner.CallCount);
            Assert.Single(context.Assertions);
            Assert.Same(TestFusioner.Result, context.Assertions[0]);
        }

        [Fact]
        public async Task ExecutesEachFusionerOnlyOnce()
        {
            TestFusioner.Reset();
            TestFusioner.Result = CreateAssertion();

            var thesaurus = CreateThesaurus("PDF", "Document");

            FusionEngine engine = CreateEngine(
                thesaurus,
                [
                    CreateCatalogEntry("PDF"),
                    CreateCatalogEntry("Document")
                ]);

            var context = CreateContext();

            await engine.FuseAnalysis(context);

            Assert.Equal(1, TestFusioner.CallCount);
            Assert.Single(context.Assertions);
        }

        [Fact]
        public async Task EmitsFusionerSummary_WhenAssertionIsProduced()
        {
            TestFusioner.Reset();

            TestFusioner.Result = CreateAssertion(
                "Document format",
                [new("Format", "PDF")]);

            FusionEngine engine = CreateEngine(
                CreateThesaurus("PDF"),
                [CreateCatalogEntry("PDF")]);

            IAnalysisMessage? receivedMessage = null;
            engine.AnalyserMessageEvent += message => receivedMessage = message;

            await engine.FuseAnalysis(CreateContext());

            Assert.NotNull(receivedMessage);
            Assert.Equal(
                AnalysisMessageTypeEnum.FusionerSummary,
                receivedMessage.MessageType);

            Assert.Equal(
                "Document format",
                receivedMessage.Information?.Label);
        }

        [Fact]
        public async Task IgnoresFusionersThatDoNotMatchAnalysisTags()
        {
            TestFusioner.Reset();
            TestFusioner.Result = CreateAssertion();

            FusionEngine engine = CreateEngine(
                CreateThesaurus("PDF"),
                [CreateCatalogEntry("ZIP")]);

            var context = CreateContext();

            await engine.FuseAnalysis(context);

            Assert.Equal(0, TestFusioner.CallCount);
            Assert.Empty(context.Assertions);
        }

        private static FusionEngine CreateEngine(
            Mock<IThesaurusService> thesaurus,
            List<ICatalogEntry> catalog)
        {
            return new FusionEngine(
                Mock.Of<IMessageCenter>(),
                Mock.Of<ILogger>(),
                Mock.Of<ISettingsService>(),
                new Factory(),
                thesaurus.Object,
                catalog);
        }

        private static Mock<IThesaurusService> CreateThesaurus(
            params string[] tags)
        {
            var thesaurus = new Mock<IThesaurusService>();

            thesaurus
                .Setup(x => x.GetAncestorPreferredTerms(It.IsAny<IFileFormat>()))
                .ReturnsAsync(tags.ToList());

            return thesaurus;
        }

        private static AnalysisContext CreateContext()
        {
            return new AnalysisContext
            {
                FilePath = "test.pdf",
                CurrentFileFormat = new FileFormat
                {
                    Referentiel = "PRONOM",
                    Label = "fmt/276"
                }
            };
        }

        private static CatalogEntry CreateCatalogEntry(string tag)
        {
            Type type = typeof(TestFusioner);

            return new CatalogEntry
            {
                InterfaceType = "IFusioner",
                Label = "Test fusioner",
                ClassName = type.FullName!,
                Tags = [tag],
                AssemblyPath = type.Assembly.Location,
                Assembly = type.Assembly,
                ModuleName = "Test",
                ModuleVersion = "1.0"
            };
        }

        private static Assertion CreateAssertion(
            string type = "Test assertion",
            List<KeyValuePair<string, string>>? summary = null)
        {
            return new Assertion
            {
                SourceModule = "Test",
                SourceContribution = "TestFusioner",
                AssertionType = type,
                ClaimJson = "{}",
                Summary = summary ?? []
            };
        }

        public class TestFusioner : IFusioner
        {
            public static int CallCount { get; private set; }
            public static IAssertion Result { get; set; } = null!;

            public TestFusioner(
                IMessageCenter messageCenter,
                ILogger logger,
                ISettingsService settingsService,
                IEntityFactory entityFactory)
            {
            }

            public Task<IAssertion> Fuse(IAnalysisContext analysisContext)
            {
                CallCount++;
                return Task.FromResult(Result);
            }

            public static void Reset()
            {
                CallCount = 0;
                Result = null!;
            }
        }
    }
}