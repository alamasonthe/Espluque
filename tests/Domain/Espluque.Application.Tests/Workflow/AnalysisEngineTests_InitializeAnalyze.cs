using Espluque.Application.Workflow;
using Espluque.Contracts.Catalog;
using Espluque.Contracts.Contributions;
using Espluque.Contracts.CrossCutting;
using Espluque.Contracts.Thesaurus;
using Espluque.Contracts.Workflow;
using Moq;

namespace Espluque.Application.Tests.Workflow
{
    public class AnalysisEngineTests_InitializeAnalyze
    {
        [Fact]
        public async Task UsesDefaultStartingTag_WhenStartingTagIsMissing()
        {
            Mock<IEntityFactory> entityFactory = new();
            Mock<IFileFormat> initialFormat = new();

            entityFactory
                .Setup(x => x.CreateFileFormat("Espluque", "AnyFile", null, null))
                .Returns(initialFormat.Object);

            AnalysisEngine engine = new(
                Mock.Of<IMessageCenter>(),
                Mock.Of<ILogger>(),
                Mock.Of<ISettingsService>(),
                entityFactory.Object,
                Mock.Of<IThesaurusService>(),
                new List<ICatalogEntry>());

            AnalysisContext context = new()
            {
                StartingTag = null,
                FilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "missing.bin")
            };

            IAnalysisContext result = await engine.AnalyzeFileAsync(context);

            Assert.Equal("AnyFile", result.StartingTag);
            Assert.Same(initialFormat.Object, result.CurrentFileFormat);
            entityFactory.Verify(x => x.CreateFileFormat("Espluque", "AnyFile", null, null), Times.Once);
        }
    }
}