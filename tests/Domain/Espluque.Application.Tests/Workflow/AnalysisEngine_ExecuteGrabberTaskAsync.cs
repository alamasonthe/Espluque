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
    public class AnalysisEngine_ExecuteGrabberTaskAsync
    {
        [Fact]
        public async Task ExecutesMatchingGrabber_AddsObservedData_AndEmitsResult()
        {
            string filePath = Path.GetTempFileName();
            try
            {
                Mock<IEntityFactory> entityFactory = new();
                Mock<IFileInformationPack> informationPack = new();
                entityFactory.Setup(x => x.CreateFileFormat("Espluque", "AnyFile", null, null))
                    .Returns(new FileFormat { Referentiel = "Espluque", Label = "AnyFile" });
                entityFactory.Setup(x => x.CreateFileInformationPack("Test grabber", It.IsAny<List<KeyValuePair<string, string>>>()))
                    .Returns(informationPack.Object);

                Mock<IThesaurusService> thesaurus = new();
                thesaurus.Setup(x => x.GetConceptMainTermByTerm("Espluque", "AnyFile"))
                    .ReturnsAsync(((int ConceptId, string MainTerm)?)null);
                thesaurus.Setup(x => x.GetAncestorPreferredTerms(It.IsAny<IFileFormat>()))
                    .ReturnsAsync((List<string>?)null);

                Type grabberType = typeof(TestGrabber);
                List<ICatalogEntry> catalog =
                [
                    new CatalogEntry
                    {
                        InterfaceType = "IGrabber",
                        Label = "Test grabber",
                        ClassName = grabberType.FullName!,
                        Tags = ["AnyFile"],
                        AssemblyPath = grabberType.Assembly.Location,
                        Assembly = grabberType.Assembly,
                        ModuleName = "Test module",
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

                AnalysisContext context = new() { FilePath = filePath };
                await engine.AnalyzeFileAsync(context);

                IGrabberResult result = Assert.Single(context.ObservedData);
                Assert.Equal("Test module", result.ModuleName);
                Assert.Equal("Test grabber", result.ContributionLabel);
                Assert.Equal("Value", Assert.Single(result.GrabbedInformation).Value);

                IAnalysisMessage message = Assert.Single(messages);
                Assert.Equal(AnalysisMessageTypeEnum.GrabberResult, message.MessageType);
                Assert.Same(informationPack.Object, message.Information);
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        public class TestGrabber : IGrabber
        {
            public TestGrabber(IMessageCenter messageCenter, ILogger logger, ISettingsService settingsService, IEntityFactory entityFactory)
            {
            }

            public Task<List<KeyValuePair<string, string>>> Grab(IAnalysisContext analysisContext)
                => Task.FromResult<List<KeyValuePair<string, string>>>([new("Key", "Value")]);
        }
    }
}