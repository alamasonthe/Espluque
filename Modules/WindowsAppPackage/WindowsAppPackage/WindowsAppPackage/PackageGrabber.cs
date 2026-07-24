using Espluque.Contracts.Enums;
using Espluque.Contracts.Interfaces;
using Espluque.Contracts.MessageInterfaces;
using Espluque.Contracts.ModuleInterfaces.Contributions;
using Espluque.Contracts.Ports;
using Microsoft.Extensions.Logging;
using SevenZip.Services;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Util;

namespace WindowsAppPackage
{
    public class PackageGrabber : IGrabber
    {
        private readonly IMessageCenter _messageCenter;
        private readonly Espluque.Contracts.Ports.ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        public PackageGrabber(IMessageCenter messageCenter,
            Espluque.Contracts.Ports.ILogger logger,
            ISettingsService settingsService,
            IEntityFactory entityFactory)
        {
            _messageCenter = messageCenter;
            _logger = logger;
            _settingsService = settingsService;
            _entityFactory = entityFactory;
        }

        public Task<List<KeyValuePair<string, string>>> Grab(string filePath)
        {
            List<KeyValuePair<string, string>> properties = [];

            Result<string> manifestResult = SevenZipService.ExtractEntryToString(filePath, "AppxManifest.xml");
            if (!manifestResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"{Path.GetFileName(filePath)}\t{manifestResult.Error.Code} {manifestResult.Error.Message}");
                return Task.FromResult(properties);
            }

            try
            {
                XDocument document = XDocument.Parse(manifestResult.Value);
                XElement? root = document.Root;

                if (root?.Name.LocalName != "Package")
                    return Task.FromResult(properties);

                XNamespace ns = root.Name.Namespace;
                XElement? identity = root.Element(ns + "Identity");
                XElement? manifestProperties = root.Element(ns + "Properties");

                string? name = (string?)identity?.Attribute("Name");
                string? publisher = (string?)identity?.Attribute("Publisher");
                string? version = (string?)identity?.Attribute("Version");
                string architecture = (string?)identity?.Attribute("ProcessorArchitecture") ?? "neutral";
                string resourceId = (string?)identity?.Attribute("ResourceId") ?? string.Empty;

                Add(properties, "Name", name);
                Add(properties, "Publisher", publisher);
                Add(properties, "Version", version);
                Add(properties, "Architecture", architecture);
                Add(properties, "ResourceId", resourceId);

                if (!string.IsNullOrWhiteSpace(name) &&
                    !string.IsNullOrWhiteSpace(publisher) &&
                    !string.IsNullOrWhiteSpace(version))
                {
                    string publisherId = GetPublisherId(publisher);
                    string packageFamilyName = $"{name}_{publisherId}";
                    string packageFullName = $"{name}_{version}_{architecture}_{resourceId}_{publisherId}";

                    Add(properties, "PublisherId", publisherId);
                    Add(properties, "PackageFamilyName", packageFamilyName);
                    Add(properties, "PackageFullName", packageFullName);
                }

                Add(properties, "DisplayName", manifestProperties?.Elements().FirstOrDefault(element => element.Name.LocalName == "DisplayName")?.Value);
                Add(properties, "PublisherDisplayName", manifestProperties?.Elements().FirstOrDefault(element => element.Name.LocalName == "PublisherDisplayName")?.Value);
                Add(properties, "Description", manifestProperties?.Elements().FirstOrDefault(element => element.Name.LocalName == "Description")?.Value);

                List<XElement> applications = root
                    .Descendants()
                    .Where(element => element.Name.LocalName == "Application")
                    .ToList();

                properties.Add(new("ApplicationsCount", applications.Count.ToString()));

                string applicationIds = string.Join(", ", applications
                    .Select(application => (string?)application.Attribute("Id"))
                    .Where(applicationId => !string.IsNullOrWhiteSpace(applicationId)));

                Add(properties, "ApplicationIds", applicationIds);

                List<string> minimumVersions = root
                    .Descendants()
                    .Where(element => element.Name.LocalName == "TargetDeviceFamily")
                    .Select(element => (string?)element.Attribute("MinVersion"))
                    .Concat(root.Descendants().Where(element => element.Name.LocalName == "OSMinVersion").Select(element => element.Value))
                    .Where(version => !string.IsNullOrWhiteSpace(version))
                    .Select(version => version!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                List<string> maximumVersions = root
                    .Descendants()
                    .Where(element => element.Name.LocalName == "TargetDeviceFamily")
                    .Select(element => (string?)element.Attribute("MaxVersionTested"))
                    .Concat(root.Descendants().Where(element => element.Name.LocalName == "OSMaxVersionTested").Select(element => element.Value))
                    .Where(version => !string.IsNullOrWhiteSpace(version))
                    .Select(version => version!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                Add(properties, "MinimumOSVersion", string.Join(", ", minimumVersions));
                Add(properties, "MaximumOSVersionTested", string.Join(", ", maximumVersions));

                return Task.FromResult(properties);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"{Path.GetFileName(filePath)}\tFailed to parse AppxManifest.xml: {ex.Message}");
                return Task.FromResult(properties);
            }
        }

        private static string GetPublisherId(string publisher)
        {
            using SHA256 sha256 = SHA256.Create();

            byte[] hash = sha256.ComputeHash(Encoding.Unicode.GetBytes(publisher));
            string binary = string.Concat(hash.Take(8).Select(value => Convert.ToString(value, 2).PadLeft(8, '0'))) + '0';

            const string alphabet = "0123456789abcdefghjkmnpqrstvwxyz";

            return string.Concat(Enumerable.Range(0, 13)
                .Select(index => alphabet[Convert.ToInt32(binary.Substring(index * 5, 5), 2)]));
        }

        private static void Add(List<KeyValuePair<string, string>> properties, string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                properties.Add(new(name, value.Trim()));
        }
    }
}