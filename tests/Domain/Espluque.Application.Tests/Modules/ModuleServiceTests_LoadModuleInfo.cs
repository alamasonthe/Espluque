using Espluque.Application.Contributions;
using Espluque.Contracts.Contributions;
using Moq;

namespace Espluque.Application.Tests.Modules
{
    public class ModuleServiceTests_LoadModuleInfo
    {
        [Fact]
        public async Task LoadsModuleInfo_WhenDefinitionIsValid()
        {
            string filePath = CreateModuleInfoFile();

            try
            {
                var settingsService =
                    new Mock<IContributionSettingsService>();

                settingsService
                    .Setup(x => x.GetUserSettings(
                        "TestModule.dll",
                        "IGrabber",
                        "TestModule.TestGrabber"))
                    .ReturnsAsync((IContributionSettings?)null);

                var service =
                    new Espluque.Application.Modules.ModuleService(
                        settingsService.Object);

                var result = await service.LoadModuleInfo(filePath);

                Assert.NotNull(result);
                Assert.Equal("TestModule", result.Name);
                Assert.Equal("1.0.0", result.Version);
                Assert.Equal("TestModule.dll", result.Assembly);
                Assert.Equal(filePath, result.FilePath);

                var contribution = Assert.Single(result.Contributions);

                Assert.Equal("IGrabber", contribution.InterfaceType);
                Assert.Equal("TestModule.TestGrabber", contribution.ClassName);
            }
            finally
            {
                DeleteModuleInfoFile(filePath);
            }
        }

        [Fact]
        public async Task AppliesUserSettings_WhenOverrideExists()
        {
            string filePath = CreateModuleInfoFile();

            try
            {
                var settingsService =
                    new Mock<IContributionSettingsService>();

                var userSettings = new ContributionSettings
                {
                    Active = false,
                    Tags = ["UserTag"]
                };

                settingsService
                    .Setup(x => x.GetUserSettings(
                        "TestModule.dll",
                        "IGrabber",
                        "TestModule.TestGrabber"))
                    .ReturnsAsync(userSettings);

                var service =
                    new Espluque.Application.Modules.ModuleService(
                        settingsService.Object);

                var result = await service.LoadModuleInfo(filePath);

                Assert.NotNull(result);

                var contribution = Assert.Single(result.Contributions);

                Assert.False(contribution.ContributionSettings.Active);
                Assert.Equal(
                    ["UserTag"],
                    contribution.ContributionSettings.Tags);
            }
            finally
            {
                DeleteModuleInfoFile(filePath);
            }
        }

        [Fact]
        public async Task KeepsModuleSettings_WhenNoOverrideExists()
        {
            string filePath = CreateModuleInfoFile();

            try
            {
                var settingsService =
                    new Mock<IContributionSettingsService>();

                settingsService
                    .Setup(x => x.GetUserSettings(
                        "TestModule.dll",
                        "IGrabber",
                        "TestModule.TestGrabber"))
                    .ReturnsAsync((IContributionSettings?)null);

                var service =
                    new Espluque.Application.Modules.ModuleService(
                        settingsService.Object);

                var result = await service.LoadModuleInfo(filePath);

                Assert.NotNull(result);

                var contribution = Assert.Single(result.Contributions);

                Assert.True(contribution.ContributionSettings.Active);
                Assert.Equal(
                    ["DefaultTag"],
                    contribution.ContributionSettings.Tags);
            }
            finally
            {
                DeleteModuleInfoFile(filePath);
            }
        }

        [Fact]
        public async Task ReturnsNull_WhenDefinitionCannotBeLoaded()
        {
            string filePath = CreateModuleInfoFile("{ invalid json");

            try
            {
                var settingsService =
                    new Mock<IContributionSettingsService>();

                var service =
                    new Espluque.Application.Modules.ModuleService(
                        settingsService.Object);

                var result = await service.LoadModuleInfo(filePath);

                Assert.Null(result);
            }
            finally
            {
                DeleteModuleInfoFile(filePath);
            }
        }

        private static string CreateModuleInfoFile(
            string? json = null)
        {
            string directoryPath = Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString());

            Directory.CreateDirectory(directoryPath);

            string filePath =
                Path.Combine(directoryPath, "module.json");

            File.WriteAllText(
                filePath,
                json ?? ValidModuleJson());

            return filePath;
        }

        private static void DeleteModuleInfoFile(string filePath)
        {
            string? directoryPath =
                Path.GetDirectoryName(filePath);

            if (directoryPath is not null &&
                Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }
        }

        private static string ValidModuleJson()
        {
            return """
            {
              "name": "TestModule",
              "version": "1.0.0",
              "author": "Test",
              "description": "Test module",
              "assembly": "TestModule.dll",
              "contributions": [
                {
                  "interfaceType": "IGrabber",
                  "label": "Test Grabber",
                  "description": "Test contribution",
                  "className": "TestModule.TestGrabber",
                  "contributionSettings": {
                    "active": true,
                    "tags": [
                      "DefaultTag"
                    ]
                  }
                }
              ]
            }
            """;
        }
    }
}