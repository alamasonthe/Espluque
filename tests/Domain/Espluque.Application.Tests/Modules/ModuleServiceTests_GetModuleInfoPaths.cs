using Espluque.Application.Modules;
using Moq;
using Espluque.Contracts.CrossCutting;

namespace Espluque.Application.Tests.Modules
{
    public class ModuleServiceTests_GetModuleInfoPaths
    {
        [Fact]
        public void ReturnsModuleInfoPaths_Recursively()
        {
            string rootPath = Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString());

            string firstModulePath = Path.Combine(rootPath, "Module1");
            string secondModulePath = Path.Combine(rootPath, "Group", "Module2");

            Directory.CreateDirectory(firstModulePath);
            Directory.CreateDirectory(secondModulePath);

            string firstModuleInfoPath =
                Path.Combine(firstModulePath, "module.json");

            string secondModuleInfoPath =
                Path.Combine(secondModulePath, "module.json");

            try
            {
                File.WriteAllText(firstModuleInfoPath, "{}");
                File.WriteAllText(secondModuleInfoPath, "{}");
                File.WriteAllText(
                    Path.Combine(secondModulePath, "other.json"),
                    "{}");

                var contributionSettingsService =
                    new Mock<Contracts.Contributions.IContributionSettingsService>();

                var logger = new Mock<ILogger>();

                var service =
                    new ModuleService( contributionSettingsService.Object, logger.Object);

                List<string> result =
                    service.GetModuleInfoPaths(rootPath);

                Assert.Equal(2, result.Count);
                Assert.Contains(firstModuleInfoPath, result);
                Assert.Contains(secondModuleInfoPath, result);
            }
            finally
            {
                if (Directory.Exists(rootPath))
                {
                    Directory.Delete(rootPath, true);
                }
            }
        }
    }
}