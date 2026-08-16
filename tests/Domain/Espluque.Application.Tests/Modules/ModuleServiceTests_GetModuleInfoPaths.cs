using Moq;

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

                var service =
                    new Espluque.Application.Modules.ModuleService(
                        contributionSettingsService.Object);

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