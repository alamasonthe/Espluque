using Espluque.Application.Contributions;
using Espluque.Application.Modules;
using Espluque.Contracts.Contributions;
using Moq;

namespace Espluque.Application.Tests.Modules
{
    public class ModuleServiceTests_SaveModuleInfo
    {
        [Fact]
        public async Task SavesModuleInfo()
        {
            string directoryPath = CreateTempDirectory();
            string filePath = Path.Combine(directoryPath, "module.json");

            try
            {
                var settingsService =
                    new Mock<IContributionSettingsService>();

                var service =
                    new ModuleService(settingsService.Object);

                var moduleInfo = CreateModuleInfo(filePath);

                bool result = await service.SaveModuleInfo(moduleInfo);

                Assert.True(result);
                Assert.True(File.Exists(filePath));

                string json = await File.ReadAllTextAsync(filePath);

                Assert.Contains("\"name\": \"TestModule\"", json);
                Assert.Contains("\"assembly\": \"TestModule.dll\"", json);
                Assert.Contains("\"interfaceType\": \"IGrabber\"", json);
                Assert.Contains("\"active\": true", json);
                Assert.Contains("\"DefaultTag\"", json);
            }
            finally
            {
                DeleteTempDirectory(directoryPath);
            }
        }

        [Fact]
        public async Task UsesSpecifiedFilePath_WhenProvided()
        {
            string directoryPath = CreateTempDirectory();

            string defaultFilePath =
                Path.Combine(directoryPath, "default.json");

            string specifiedFilePath =
                Path.Combine(directoryPath, "specified.json");

            try
            {
                var settingsService =
                    new Mock<IContributionSettingsService>();

                var service =
                    new ModuleService(settingsService.Object);

                var moduleInfo = CreateModuleInfo(defaultFilePath);

                bool result =
                    await service.SaveModuleInfo(
                        moduleInfo,
                        specifiedFilePath);

                Assert.True(result);

                Assert.True(File.Exists(specifiedFilePath));
                Assert.False(File.Exists(defaultFilePath));
            }
            finally
            {
                DeleteTempDirectory(directoryPath);
            }
        }

        [Fact]
        public async Task ReturnsFalse_WhenModuleInfoCannotBeSaved()
        {
            string directoryPath = CreateTempDirectory();

            try
            {
                string invalidFilePath = Path.Combine(
                    directoryPath,
                    "MissingDirectory",
                    "module.json");

                var settingsService =
                    new Mock<IContributionSettingsService>();

                var service =
                    new ModuleService(settingsService.Object);

                var moduleInfo =
                    CreateModuleInfo(invalidFilePath);

                bool result =
                    await service.SaveModuleInfo(moduleInfo);

                Assert.False(result);
            }
            finally
            {
                DeleteTempDirectory(directoryPath);
            }
        }

        private static ModuleInfo CreateModuleInfo(string filePath)
        {
            return new ModuleInfo
            {
                FilePath = filePath,
                Name = "TestModule",
                Version = "1.0.0",
                Author = "Test",
                Description = "Test module",
                Assembly = "TestModule.dll",
                Contributions =
                [
                    new ModuleContributionInfo
                    {
                        InterfaceType = "IGrabber",
                        Label = "Test Grabber",
                        Description = "Test contribution",
                        ClassName = "TestModule.TestGrabber",
                        ContributionSettings = new ContributionSettings
                        {
                            Active = true,
                            Tags = ["DefaultTag"]
                        }
                    }
                ]
            };
        }

        private static string CreateTempDirectory()
        {
            string directoryPath = Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString());

            Directory.CreateDirectory(directoryPath);

            return directoryPath;
        }

        private static void DeleteTempDirectory(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }
        }
    }
}