using Espluque.Application.Catalog;
using Espluque.Contracts.Catalog;
using Espluque.Contracts.Contributions;
using Espluque.Contracts.Modules;
using Moq;

namespace Espluque.Application.Tests.Catalog
{
    public class CatalogServiceTests
    {
        [Fact]
        public async Task BuildAsync_ReturnsCatalogEntries_ForValidActiveContributions()
        {
            string assemblyPath = typeof(CatalogService).Assembly.Location;
            string assemblyDirectory = Path.GetDirectoryName(assemblyPath)!;
            string moduleInfoPath = Path.Combine(assemblyDirectory, "module.json");

            Mock<IModuleContributionInfo> contribution =
                CreateContribution(
                    interfaceType: "IDetector",
                    className: "Test.Detector",
                    active: true,
                    tags: ["Markdown"]);

            Mock<IModuleInfo> moduleInfo =
                CreateModuleInfo(
                    Path.GetFileName(assemblyPath),
                    contribution.Object);

            Mock<IModuleService> moduleService = new();

            moduleService
                .Setup(x => x.GetModuleInfoPaths("modules"))
                .Returns([moduleInfoPath]);

            moduleService
                .Setup(x => x.LoadModuleInfo(moduleInfoPath))
                .ReturnsAsync(moduleInfo.Object);

            CatalogService service = new(moduleService.Object);

            List<ICatalogEntry> result =
                await service.BuildAsync("modules");

            ICatalogEntry entry = Assert.Single(result);

            Assert.Equal("IDetector", entry.InterfaceType);
            Assert.Equal("Test.Detector", entry.ClassName);
            Assert.Equal("Detector", entry.Label);
            Assert.Contains("Markdown", entry.Tags);
            Assert.Equal("TestModule", entry.ModuleName);
            Assert.Equal("1.0", entry.ModuleVersion);
            Assert.NotNull(entry.Assembly);
        }


        [Fact]
        public async Task BuildAsync_IgnoresInactiveContributions()
        {
            string assemblyPath = typeof(CatalogService).Assembly.Location;
            string assemblyDirectory = Path.GetDirectoryName(assemblyPath)!;
            string moduleInfoPath = Path.Combine(assemblyDirectory, "module.json");

            Mock<IModuleContributionInfo> contribution =
                CreateContribution(
                    interfaceType: "IDetector",
                    className: "Test.Detector",
                    active: false,
                    tags: ["Markdown"]);

            Mock<IModuleInfo> moduleInfo =
                CreateModuleInfo(
                    Path.GetFileName(assemblyPath),
                    contribution.Object);

            Mock<IModuleService> moduleService = new();

            moduleService
                .Setup(x => x.GetModuleInfoPaths("modules"))
                .Returns([moduleInfoPath]);

            moduleService
                .Setup(x => x.LoadModuleInfo(moduleInfoPath))
                .ReturnsAsync(moduleInfo.Object);

            CatalogService service = new(moduleService.Object);

            List<ICatalogEntry> result =
                await service.BuildAsync("modules");

            Assert.Empty(result);
        }


        [Fact]
        public async Task BuildAsync_IgnoresInvalidContributions()
        {
            string assemblyPath = typeof(CatalogService).Assembly.Location;
            string assemblyDirectory = Path.GetDirectoryName(assemblyPath)!;
            string moduleInfoPath = Path.Combine(assemblyDirectory, "module.json");

            Mock<IModuleContributionInfo> validContribution =
                CreateContribution(
                    interfaceType: "IDetector",
                    className: "Test.Detector",
                    active: true);

            Mock<IModuleContributionInfo> missingInterface =
                CreateContribution(
                    interfaceType: "",
                    className: "Test.Invalid1",
                    active: true);

            Mock<IModuleContributionInfo> missingClassName =
                CreateContribution(
                    interfaceType: "IGrabber",
                    className: "",
                    active: true);

            Mock<IModuleInfo> moduleInfo =
                CreateModuleInfo(
                    Path.GetFileName(assemblyPath),
                    validContribution.Object,
                    missingInterface.Object,
                    missingClassName.Object);

            Mock<IModuleService> moduleService = new();

            moduleService
                .Setup(x => x.GetModuleInfoPaths("modules"))
                .Returns([moduleInfoPath]);

            moduleService
                .Setup(x => x.LoadModuleInfo(moduleInfoPath))
                .ReturnsAsync(moduleInfo.Object);

            CatalogService service = new(moduleService.Object);

            List<ICatalogEntry> result =
                await service.BuildAsync("modules");

            ICatalogEntry entry = Assert.Single(result);

            Assert.Equal("Test.Detector", entry.ClassName);
        }


        [Fact]
        public void FilterCatalog_ReturnsMatchingEntries_CaseInsensitive()
        {
            List<ICatalogEntry> catalog =
            [
                CreateCatalogEntry(
                    interfaceType: "IDetector",
                    className: "Test.Detector",
                    tags: ["Markdown"])
            ];

            List<ICatalogEntry> result =
                CatalogService.FilterCatalog(
                    catalog,
                    "idetector",
                    "markdown");

            Assert.Single(result);
        }


        [Fact]
        public void FilterCatalog_ReturnsEmptyList_WhenNoEntryMatches()
        {
            List<ICatalogEntry> catalog =
            [
                CreateCatalogEntry(
                    interfaceType: "IDetector",
                    className: "Test.Detector",
                    tags: ["Markdown"])
            ];

            List<ICatalogEntry> result =
                CatalogService.FilterCatalog(
                    catalog,
                    "IGrabber",
                    "PDF");

            Assert.Empty(result);
        }


        private static Mock<IModuleContributionInfo> CreateContribution(
            string interfaceType,
            string className,
            bool active,
            List<string>? tags = null)
        {
            Mock<IContributionSettings> settings = new();

            settings
                .SetupGet(x => x.Active)
                .Returns(active);

            settings
                .SetupGet(x => x.Tags)
                .Returns(tags ?? []);

            Mock<IModuleContributionInfo> contribution = new();

            contribution
                .SetupGet(x => x.InterfaceType)
                .Returns(interfaceType);

            contribution
                .SetupGet(x => x.ClassName)
                .Returns(className);

            contribution
                .SetupGet(x => x.Label)
                .Returns("Detector");

            contribution
                .SetupGet(x => x.ContributionSettings)
                .Returns(settings.Object);

            return contribution;
        }


        private static Mock<IModuleInfo> CreateModuleInfo(
            string assembly,
            params IModuleContributionInfo[] contributions)
        {
            Mock<IModuleInfo> moduleInfo = new();

            moduleInfo
                .SetupGet(x => x.Name)
                .Returns("TestModule");

            moduleInfo
                .SetupGet(x => x.Version)
                .Returns("1.0");

            moduleInfo
                .SetupGet(x => x.Assembly)
                .Returns(assembly);

            moduleInfo
                .SetupGet(x => x.Contributions)
                .Returns(contributions.ToList());

            return moduleInfo;
        }


        private static ICatalogEntry CreateCatalogEntry(
            string interfaceType,
            string className,
            List<string> tags)
        {
            return new CatalogEntry
            {
                InterfaceType = interfaceType,
                Label = "Test",
                ClassName = className,
                Tags = tags,
                AssemblyPath = "",
                ModuleName = "TestModule",
                ModuleVersion = "1.0"
            };
        }
    }
}