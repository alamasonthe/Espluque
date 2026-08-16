using Espluque.Application.Contributions;
using Espluque.Contracts.Contributions;
using Espluque.Contracts.CrossCutting;
using Moq;
using System.Text.Json;

namespace Espluque.Application.Tests.Contributions
{
    public class ContributionSettingsServiceTests
    {
        [Fact]
        public async Task ReturnsEmptyList_WhenNoSettingsArePersisted()
        {
            var settingsService = new Mock<ISettingsService>();

            settingsService
                .Setup(x => x.GetJsonSectionSettings(
                    ContributionSettingsService.SettingsSectionName))
                .ReturnsAsync((string?)null);

            var service =
                new ContributionSettingsService(settingsService.Object);

            List<IContributionSettingsEntry> result =
                await service.GetUserSettingsList();

            Assert.Empty(result);
        }

        [Fact]
        public async Task ReturnsPersistedSettings()
        {
            var settingsService = new Mock<ISettingsService>();

            settingsService
                .Setup(x => x.GetJsonSectionSettings(
                    ContributionSettingsService.SettingsSectionName))
                .ReturnsAsync(CreateSettingsJson(
                    "TestModule.dll",
                    "IGrabber",
                    "TestModule.TestGrabber",
                    false,
                    ["Tag1", "Tag2"]));

            var service =
                new ContributionSettingsService(settingsService.Object);

            List<IContributionSettingsEntry> result =
                await service.GetUserSettingsList();

            IContributionSettingsEntry entry =
                Assert.Single(result);

            Assert.Equal("TestModule.dll", entry.ModuleAssembly);
            Assert.Equal("IGrabber", entry.InterfaceType);
            Assert.Equal(
                "TestModule.TestGrabber",
                entry.ClassName);

            Assert.False(entry.Settings.Active);
            Assert.Equal(
                ["Tag1", "Tag2"],
                entry.Settings.Tags);
        }

        [Fact]
        public async Task ReturnsSettings_WhenContributionExists()
        {
            var settingsService = new Mock<ISettingsService>();

            settingsService
                .Setup(x => x.GetJsonSectionSettings(
                    ContributionSettingsService.SettingsSectionName))
                .ReturnsAsync(CreateSettingsJson(
                    "TestModule.dll",
                    "IGrabber",
                    "TestModule.TestGrabber",
                    false,
                    ["UserTag"]));

            var service =
                new ContributionSettingsService(settingsService.Object);

            IContributionSettings? result =
                await service.GetUserSettings(
                    "TestModule.dll",
                    "IGrabber",
                    "TestModule.TestGrabber");

            Assert.NotNull(result);
            Assert.False(result.Active);
            Assert.Equal(["UserTag"], result.Tags);
        }

        [Fact]
        public async Task ReturnsNull_WhenContributionDoesNotExist()
        {
            var settingsService = new Mock<ISettingsService>();

            settingsService
                .Setup(x => x.GetJsonSectionSettings(
                    ContributionSettingsService.SettingsSectionName))
                .ReturnsAsync(CreateSettingsJson(
                    "TestModule.dll",
                    "IGrabber",
                    "TestModule.TestGrabber",
                    true,
                    ["Tag"]));

            var service =
                new ContributionSettingsService(settingsService.Object);

            IContributionSettings? result =
                await service.GetUserSettings(
                    "OtherModule.dll",
                    "IGrabber",
                    "OtherModule.TestGrabber");

            Assert.Null(result);
        }

        [Fact]
        public async Task AddsSettings_WhenContributionDoesNotExist()
        {
            var settingsService = new Mock<ISettingsService>();

            settingsService
                .Setup(x => x.GetJsonSectionSettings(
                    ContributionSettingsService.SettingsSectionName))
                .ReturnsAsync("[]");

            string? savedPayload = null;

            settingsService
                .Setup(x => x.SaveJsonSectionSettings(
                    ContributionSettingsService.SettingsSectionName,
                    It.IsAny<string>()))
                .Callback<string, string>(
                    (_, payload) => savedPayload = payload)
                .ReturnsAsync(true);

            var service =
                new ContributionSettingsService(settingsService.Object);

            var newSettings = new ContributionSettings
            {
                Active = false,
                Tags = ["NewTag"]
            };

            bool result = await service.SaveUserSettings(
                "TestModule.dll",
                "IGrabber",
                "TestModule.TestGrabber",
                newSettings);

            Assert.True(result);
            Assert.NotNull(savedPayload);

            using JsonDocument document =
                JsonDocument.Parse(savedPayload);

            JsonElement entries =
                document.RootElement;

            Assert.Equal(1, entries.GetArrayLength());

            JsonElement entry = entries[0];

            Assert.Equal(
                "TestModule.dll",
                entry.GetProperty("ModuleAssembly").GetString());

            Assert.Equal(
                "IGrabber",
                entry.GetProperty("InterfaceType").GetString());

            Assert.Equal(
                "TestModule.TestGrabber",
                entry.GetProperty("ClassName").GetString());

            JsonElement persistedSettings =
                entry.GetProperty("Settings");

            Assert.False(
                persistedSettings.GetProperty("Active").GetBoolean());

            Assert.Equal(
                "NewTag",
                persistedSettings
                    .GetProperty("Tags")[0]
                    .GetString());
        }

        [Fact]
        public async Task UpdatesSettings_WhenContributionAlreadyExists()
        {
            var settingsService = new Mock<ISettingsService>();

            settingsService
                .Setup(x => x.GetJsonSectionSettings(
                    ContributionSettingsService.SettingsSectionName))
                .ReturnsAsync(CreateSettingsJson(
                    "TestModule.dll",
                    "IGrabber",
                    "TestModule.TestGrabber",
                    true,
                    ["OldTag"]));

            string? savedPayload = null;

            settingsService
                .Setup(x => x.SaveJsonSectionSettings(
                    ContributionSettingsService.SettingsSectionName,
                    It.IsAny<string>()))
                .Callback<string, string>(
                    (_, payload) => savedPayload = payload)
                .ReturnsAsync(true);

            var service =
                new ContributionSettingsService(settingsService.Object);

            var newSettings = new ContributionSettings
            {
                Active = false,
                Tags = ["NewTag"]
            };

            bool result = await service.SaveUserSettings(
                "TestModule.dll",
                "IGrabber",
                "TestModule.TestGrabber",
                newSettings);

            Assert.True(result);
            Assert.NotNull(savedPayload);

            using JsonDocument document =
                JsonDocument.Parse(savedPayload);

            JsonElement entries =
                document.RootElement;

            Assert.Equal(1, entries.GetArrayLength());

            JsonElement entry = entries[0];
            JsonElement persistedSettings =
                entry.GetProperty("Settings");

            Assert.False(
                persistedSettings.GetProperty("Active").GetBoolean());

            Assert.Equal(
                "NewTag",
                persistedSettings
                    .GetProperty("Tags")[0]
                    .GetString());
        }

        private static string CreateSettingsJson(
            string moduleAssembly,
            string interfaceType,
            string className,
            bool active,
            string[] tags)
        {
            return JsonSerializer.Serialize(
                new[]
                {
                    new
                    {
                        ModuleAssembly = moduleAssembly,
                        InterfaceType = interfaceType,
                        ClassName = className,
                        Settings = new
                        {
                            Active = active,
                            Tags = tags
                        }
                    }
                });
        }
    }
}