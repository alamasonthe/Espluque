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
    public class AnalysisEngine_UpdateBacklogsAsync
    {
        [Fact]
        public async Task ExecutesContributionsForAncestorTags_AfterFormatChanges()
        {
            TestDetector.CallCount = 0;
            TestGrabber.CallCount = 0;

            Mock<IThesaurusService> thesaurus = new();
            thesaurus.Setup(x => x.GetConceptMainTermByTerm("Espluque", "AnyFile")).ReturnsAsync((1, "AnyFile"));
            thesaurus.Setup(x => x.GetConceptMainTermByTerm("Test", "PDF")).ReturnsAsync(((int ConceptId, string MainTerm)?)null);
            thesaurus.Setup(x => x.GetAncestorPreferredTerms(It.Is<IFileFormat>(f => f.Label == "PDF"))).ReturnsAsync(["Document"]);

            Type detectorType = typeof(TestDetector);
            Type grabberType = typeof(TestGrabber);

            List<ICatalogEntry> catalog =
            [
                CreateEntry(detectorType, "IDetector", "AnyFile"),
                CreateEntry(grabberType, "IGrabber", "Document")
            ];

            AnalysisEngine engine = new(
                Mock.Of<IMessageCenter>(),
                Mock.Of<ILogger>(),
                Mock.Of<ISettingsService>(),
                new Factory(),
                thesaurus.Object,
                catalog);

            string filePath = Path.GetTempFileName();

            try
            {
                AnalysisContext context = new() { FilePath = filePath };
                await engine.AnalyzeFileAsync(context);

                Assert.Equal(1, TestDetector.CallCount);
                Assert.Equal("PDF", context.CurrentFileFormat!.Label);
                Assert.Equal(1, TestGrabber.CallCount);
                Assert.Single(context.ObservedData);
            }
            finally
            {
                File.Delete(filePath);
            }
        }

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
            public static int CallCount { get; set; }

            public TestDetector(IMessageCenter messageCenter, ILogger logger, ISettingsService settingsService, IEntityFactory entityFactory) { }

            public Task<IFileFormat?> Detect(IAnalysisContext analysisContext)
            {
                CallCount++;
                return Task.FromResult<IFileFormat?>(new FileFormat { Referentiel = "Test", Label = "PDF" });
            }
        }

        public class TestGrabber : IGrabber
        {
            public static int CallCount { get; set; }

            public TestGrabber(IMessageCenter messageCenter, ILogger logger, ISettingsService settingsService, IEntityFactory entityFactory) { }

            public Task<List<KeyValuePair<string, string>>> Grab(IAnalysisContext analysisContext)
            {
                CallCount++;
                return Task.FromResult<List<KeyValuePair<string, string>>>([new("Ancestor", "Document")]);
            }
        }
    }
}