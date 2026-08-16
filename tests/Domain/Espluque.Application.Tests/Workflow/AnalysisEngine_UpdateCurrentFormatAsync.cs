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
    public class AnalysisEngine_UpdateCurrentFormatAsync
    {
        [Fact]
        public async Task UpdatesCurrentFormat_WhenOneFormatIsDetected()
        {
            Mock<IThesaurusService> thesaurus = CreateThesaurus();
            Type detectorType = typeof(SingleFormatDetector);
            AnalysisEngine engine = CreateEngine(thesaurus, [CreateCatalogEntry(detectorType)]);
            AnalysisContext context = await ExecuteAnalysis(engine);

            Assert.Equal("PDF", context.CurrentFileFormat!.Label);
            IFileFormat previousFormat = Assert.Single(context.FileFormatHistory);
            Assert.Equal("AnyFile", previousFormat.Label);
        }

        [Fact]
        public async Task SelectsMostSpecificFormat_WhenSeveralFormatsAreDetected()
        {
            Mock<IThesaurusService> thesaurus = CreateThesaurus();
            thesaurus.Setup(x => x.GetConceptMainTermByTerm("Test", "Parent")).ReturnsAsync((10, "Parent"));
            thesaurus.Setup(x => x.GetConceptMainTermByTerm("Test", "Child")).ReturnsAsync((20, "Child"));
            thesaurus.Setup(x => x.GetConceptPathExists(10, 20)).ReturnsAsync(true);

            List<ICatalogEntry> catalog =
            [
                CreateCatalogEntry(typeof(ParentFormatDetector)),
                CreateCatalogEntry(typeof(ChildFormatDetector))
            ];

            AnalysisContext context = await ExecuteAnalysis(CreateEngine(thesaurus, catalog));

            Assert.Equal("Child", context.CurrentFileFormat!.Label);
            Assert.Equal(2, context.FileFormatHistory.Count);
        }

        private static Mock<IThesaurusService> CreateThesaurus()
        {
            Mock<IThesaurusService> thesaurus = new();
            thesaurus.Setup(x => x.GetConceptMainTermByTerm("Espluque", "AnyFile")).ReturnsAsync((1, "AnyFile"));
            thesaurus.Setup(x => x.GetAncestorPreferredTerms(It.IsAny<IFileFormat>())).ReturnsAsync((List<string>?)null);
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

        private static CatalogEntry CreateCatalogEntry(Type type) => new()
        {
            InterfaceType = "IDetector",
            Label = type.Name,
            ClassName = type.FullName!,
            Tags = ["AnyFile"],
            AssemblyPath = type.Assembly.Location,
            Assembly = type.Assembly,
            ModuleName = "Test",
            ModuleVersion = "1.0"
        };

        private static async Task<AnalysisContext> ExecuteAnalysis(AnalysisEngine engine)
        {
            string filePath = Path.GetTempFileName();
            try
            {
                AnalysisContext context = new() { FilePath = filePath };
                await engine.AnalyzeFileAsync(context);
                return context;
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        public class SingleFormatDetector : IDetector
        {
            public SingleFormatDetector(IMessageCenter messageCenter, ILogger logger, ISettingsService settingsService, IEntityFactory entityFactory) { }

            public Task<IFileFormat?> Detect(IAnalysisContext analysisContext)
                => Task.FromResult<IFileFormat?>(new FileFormat { Referentiel = "Test", Label = "PDF" });
        }

        public class ParentFormatDetector : IDetector
        {
            public ParentFormatDetector(IMessageCenter messageCenter, ILogger logger, ISettingsService settingsService, IEntityFactory entityFactory) { }

            public Task<IFileFormat?> Detect(IAnalysisContext analysisContext)
                => Task.FromResult<IFileFormat?>(new FileFormat { Referentiel = "Test", Label = "Parent" });
        }

        public class ChildFormatDetector : IDetector
        {
            public ChildFormatDetector(IMessageCenter messageCenter, ILogger logger, ISettingsService settingsService, IEntityFactory entityFactory) { }

            public Task<IFileFormat?> Detect(IAnalysisContext analysisContext)
                => Task.FromResult<IFileFormat?>(new FileFormat { Referentiel = "Test", Label = "Child" });
        }
    }
}