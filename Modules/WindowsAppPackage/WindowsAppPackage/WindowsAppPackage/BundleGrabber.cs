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
    public class BundleGrabber : IGrabber
    {
        private readonly IMessageCenter _messageCenter;
        private readonly Espluque.Contracts.Ports.ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        public BundleGrabber(IMessageCenter messageCenter,
            Espluque.Contracts.Ports.ILogger logger,
            ISettingsService settingsService,
            IEntityFactory entityFactory)
        {
            _messageCenter = messageCenter;
            _logger = logger;
            _settingsService = settingsService;
            _entityFactory = entityFactory;
        }

        public Task<List<KeyValuePair<string, string>>> Grab(IAnalysisContext analysisContext)
        {
            List<KeyValuePair<string, string>> properties = [];

            Result<string> manifestResult = SevenZipService.ExtractEntryToString(analysisContext.FilePath, @"AppxMetadata\AppxBundleManifest.xml");
            if (!manifestResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"{Path.GetFileName(analysisContext.FilePath)}\t{manifestResult.Error.Code} {manifestResult.Error.Message}");
                return Task.FromResult(properties);
            }

            try
            {
                XDocument document = XDocument.Parse(manifestResult.Value);
                XElement? root = document.Root;

                if (root?.Name.LocalName != "Bundle")
                    return Task.FromResult(properties);

                XElement? identity = root.Elements().FirstOrDefault(element => element.Name.LocalName == "Identity");

                string? name = (string?)identity?.Attribute("Name");
                string? publisher = (string?)identity?.Attribute("Publisher");
                string? version = (string?)identity?.Attribute("Version");

                Add(properties, "Name", name);
                Add(properties, "Publisher", publisher);
                Add(properties, "Version", version);

                if (!string.IsNullOrWhiteSpace(name) &&
                    !string.IsNullOrWhiteSpace(publisher) &&
                    !string.IsNullOrWhiteSpace(version))
                {
                    string publisherId = GetPublisherId(publisher);
                    string uninstallParameter = $"{name}_{version}_neutral_~_{publisherId}";

                    Add(properties, "UninstallParameter", uninstallParameter);
                }

                List<XElement> packages = root
                    .Descendants()
                    .Where(element => element.Name.LocalName == "Package")
                    .ToList();

                properties.Add(new("PackageCount", packages.Count.ToString()));
                properties.Add(new("ApplicationPackageCount", packages.Count(package => string.Equals((string?)package.Attribute("Type"), "application", StringComparison.OrdinalIgnoreCase)).ToString()));
                properties.Add(new("ResourcePackageCount", packages.Count(package => string.Equals((string?)package.Attribute("Type"), "resource", StringComparison.OrdinalIgnoreCase)).ToString()));

                Add(properties, "Architectures", JoinDistinct(packages.Select(package => (string?)package.Attribute("Architecture"))));
                Add(properties, "PackageVersions", JoinDistinct(packages.Select(package => (string?)package.Attribute("Version"))));
                Add(properties, "PackageFileNames", JoinDistinct(packages.Select(package => (string?)package.Attribute("FileName"))));

                IEnumerable<XElement> resources = packages
                    .SelectMany(package => package.Descendants())
                    .Where(element => element.Name.LocalName == "Resource");

                Add(properties, "Languages", JoinDistinct(resources.Select(resource => (string?)resource.Attribute("Language"))));
                Add(properties, "Scales", JoinDistinct(resources.Select(resource => (string?)resource.Attribute("Scale"))));

                ulong declaredContentSize = 0;

                foreach (XElement package in packages)
                {
                    if (ulong.TryParse((string?)package.Attribute("Size"), out ulong packageSize))
                        declaredContentSize += packageSize;
                }

                properties.Add(new("DeclaredContentSize", declaredContentSize.ToString()));

                return Task.FromResult(new Formatter().Format(properties));
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"{Path.GetFileName(analysisContext.FilePath)}\tFailed to parse AppxBundleManifest.xml: {ex.Message}");
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

        private static string JoinDistinct(IEnumerable<string?> values)
        {
            return string.Join(", ", values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase));
        }

        private static void Add(List<KeyValuePair<string, string>> properties, string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                properties.Add(new(name, value.Trim()));
        }
    }
}