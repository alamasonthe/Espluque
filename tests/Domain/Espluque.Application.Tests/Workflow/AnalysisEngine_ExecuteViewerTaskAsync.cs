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
    public class AnalysisEngine_ExecuteViewerTaskAsync
    {
        [Fact]
        public async Task ExecutesMatchingViewer_AndEmitsViewerMessage()
        {
            string filePath = Path.GetTempFileName();
            try
            {
                Mock<IEntityFactory> entityFactory = new();
                entityFactory.Setup(x => x.CreateFileFormat("Espluque", "AnyFile", null, null))
                    .Returns(new FileFormat { Referentiel = "Espluque", Label = "AnyFile" });

                Mock<IThesaurusService> thesaurus = new();
                thesaurus.Setup(x => x.GetConceptMainTermByTerm("Espluque", "AnyFile"))
                    .ReturnsAsync(((int ConceptId, string MainTerm)?)null);
                thesaurus.Setup(x => x.GetAncestorPreferredTerms(It.IsAny<IFileFormat>()))
                    .ReturnsAsync((List<string>?)null);

                Type viewerType = typeof(TestViewer);
                List<ICatalogEntry> catalog =
                [
                    new CatalogEntry
                    {
                        InterfaceType = "IWpfViewer",
                        Label = "Test viewer",
                        ClassName = viewerType.FullName!,
                        Tags = ["AnyFile"],
                        AssemblyPath = viewerType.Assembly.Location,
                        Assembly = viewerType.Assembly,
                        ModuleName = "Test",
                        ModuleVersion = "1.0"
                    }
                ];

                AnalysisEngine engine = new(
                    Mock.Of<IMessageCenter>(),
                    Mock.Of<ILogger>(),
                    Mock.Of<ISettingsService>(),
                    entityFactory.Object,
                    thesaurus.Object,
                    catalog);

                List<IAnalysisMessage> messages = [];
                engine.AnalyserMessageEvent += messages.Add;

                await engine.AnalyzeFileAsync(new AnalysisContext { FilePath = filePath }, "IWpfViewer");

                IAnalysisMessage message = Assert.Single(messages);
                Assert.Equal(AnalysisMessageTypeEnum.ViewerUC, message.MessageType);
                Assert.Equal("Test viewer", message.Label);
                Assert.IsType<TestViewer>(message.ViewerUC);
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        public class TestViewer : IWpfViewer
        {
            public TestViewer(IMessageCenter messageCenter, ILogger logger, ISettingsService settingsService, IEntityFactory entityFactory)
            {
            }

            public Task<object?> GetViewer(IAnalysisContext analysisContext) => Task.FromResult<object?>(null);
        }
    }
}