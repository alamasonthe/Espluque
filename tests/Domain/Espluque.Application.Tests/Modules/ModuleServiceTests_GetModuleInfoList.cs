using Espluque.Application.Modules;
using Espluque.Contracts.Contributions;
using Espluque.Contracts.CrossCutting;
using Moq;

namespace Espluque.Application.Tests.Modules
{
    public class ModuleServiceTests_GetModuleInfoList
    {
        [Fact]
        public async Task ReturnsOnlySuccessfullyLoadedModules()
        {
            string directoryPath = Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString());

            Directory.CreateDirectory(directoryPath);

            string validModulePath =
                Path.Combine(directoryPath, "valid-module.json");

            string invalidModulePath =
                Path.Combine(directoryPath, "invalid-module.json");

            try
            {
                await File.WriteAllTextAsync(
                    validModulePath,
                    ValidModuleJson());

                await File.WriteAllTextAsync(
                    invalidModulePath,
                    "{ invalid json");

                var settingsService =
                    new Mock<IContributionSettingsService>();

                settingsService
                    .Setup(x => x.GetUserSettings(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                    .ReturnsAsync((IContributionSettings?)null);

                var logger = new Mock<ILogger>();

                var service =
                    new ModuleService(settingsService.Object, logger.Object);

                var result = await service.GetModuleInfoList(
                    [validModulePath, invalidModulePath]);

                var module = Assert.Single(result);

                Assert.Equal("TestModule", module.Name);
                Assert.Equal(validModulePath, module.FilePath);
            }
            finally
            {
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, true);
                }
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