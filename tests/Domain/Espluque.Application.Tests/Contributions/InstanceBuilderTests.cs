using Espluque.Application.Catalog;
using Espluque.Application.Contributions;
using Espluque.Contracts.CrossCutting;
using Moq;

namespace Espluque.Application.Tests.Contributions
{
    public class InstanceBuilderTests
    {
        [Fact]
        public void CreatesInstance_WhenCatalogEntryIsValid()
        {
            var messageCenter = new Mock<IMessageCenter>();
            var logger = new Mock<Contracts.CrossCutting.ILogger>();
            var settingsService = new Mock<ISettingsService>();
            var entityFactory = new Mock<IEntityFactory>();

            CatalogEntry entry = CreateCatalogEntry(
                typeof(ValidContribution));

            var result = InstanceBuilder.CreateInstance(
                entry,
                messageCenter.Object,
                logger.Object,
                settingsService.Object,
                entityFactory.Object);

            Assert.NotNull(result);
            Assert.Equal("Test contribution", result.Value.label);

            ValidContribution instance =
                Assert.IsType<ValidContribution>(result.Value.instance);

            Assert.Same(messageCenter.Object, instance.MessageCenter);
            Assert.Same(logger.Object, instance.Logger);
            Assert.Same(settingsService.Object, instance.SettingsService);
            Assert.Same(entityFactory.Object, instance.EntityFactory);
        }

        [Fact]
        public void ReturnsNull_WhenContributionTypeCannotBeResolved()
        {
            var messageCenter = new Mock<IMessageCenter>();
            var logger = new Mock<Contracts.CrossCutting.ILogger>();
            var settingsService = new Mock<ISettingsService>();
            var entityFactory = new Mock<IEntityFactory>();

            CatalogEntry entry = CreateCatalogEntry(
                typeof(ValidContribution));

            entry.ClassName = "Unknown.Contribution";

            var result = InstanceBuilder.CreateInstance(
                entry,
                messageCenter.Object,
                logger.Object,
                settingsService.Object,
                entityFactory.Object);

            Assert.Null(result);
        }

        [Fact]
        public void ReturnsNull_WhenRequiredConstructorIsMissing()
        {
            var messageCenter = new Mock<IMessageCenter>();
            var logger = new Mock<Contracts.CrossCutting.ILogger>();
            var settingsService = new Mock<ISettingsService>();
            var entityFactory = new Mock<IEntityFactory>();

            CatalogEntry entry = CreateCatalogEntry(
                typeof(ContributionWithoutRequiredConstructor));

            var result = InstanceBuilder.CreateInstance(
                entry,
                messageCenter.Object,
                logger.Object,
                settingsService.Object,
                entityFactory.Object);

            Assert.Null(result);
        }

        private static CatalogEntry CreateCatalogEntry(Type contributionType)
        {
            return new CatalogEntry
            {
                InterfaceType = "ITestContribution",
                Label = "Test contribution",
                ClassName = contributionType.FullName!,
                Tags = [],
                AssemblyPath = contributionType.Assembly.Location,
                Assembly = contributionType.Assembly,
                ModuleName = "TestModule",
                ModuleVersion = "1.0.0"
            };
        }

        public class ValidContribution
        {
            public IMessageCenter MessageCenter { get; }
            public Contracts.CrossCutting.ILogger Logger { get; }
            public ISettingsService SettingsService { get; }
            public IEntityFactory EntityFactory { get; }

            public ValidContribution(
                IMessageCenter messageCenter,
                Contracts.CrossCutting.ILogger logger,
                ISettingsService settingsService,
                IEntityFactory entityFactory)
            {
                MessageCenter = messageCenter;
                Logger = logger;
                SettingsService = settingsService;
                EntityFactory = entityFactory;
            }
        }

        public class ContributionWithoutRequiredConstructor
        {
            public ContributionWithoutRequiredConstructor()
            {
            }
        }
    }
}