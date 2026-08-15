using Espluque.Contracts.Contributions;
using Espluque.Contracts.Contributions.Types;
using Espluque.Contracts.CrossCutting;
using Espluque.Contracts.Workflow;
using Microsoft.Extensions.Logging;
using SevenZip.Services;
using System.Text;
using System.Xml.Linq;
using Util;

namespace WindowsAppPackage
{
    public class Detector : IDetector
    {
        private readonly IMessageCenter _messageCenter;
        private readonly Espluque.Contracts.CrossCutting.ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        private readonly string _referentiel = "Espluque";

        private readonly List<(string EntryPath, Func<string, bool> Validator)> _packageEntries =
            [
                ("[Content_Types].xml", xmlContent => IsContentTypes(xmlContent, "/AppxManifest.xml", "application/vnd.ms-appx.manifest+xml")),
                ("AppxBlockMap.xml", IsBlockMap),
                ("AppxManifest.xml", xmlContent => IsManifest(xmlContent, "Package"))
            ];

        private readonly List<(string EntryPath, Func<string, bool> Validator)> _bundleEntries =
            [
                ("[Content_Types].xml", xmlContent => IsContentTypes(xmlContent, "/AppxMetadata/AppxBundleManifest.xml", "application/vnd.ms-appx.bundlemanifest+xml")),
                ("AppxBlockMap.xml", IsBlockMap),
                (@"AppxMetadata\AppxBundleManifest.xml", xmlContent => IsManifest(xmlContent, "Bundle"))
            ];

        public Detector(IMessageCenter messageCenter,
            Espluque.Contracts.CrossCutting.ILogger logger,
            ISettingsService settingsService,
            IEntityFactory entityFactory)
        {
            _messageCenter = messageCenter;
            _logger = logger;
            _settingsService = settingsService;
            _entityFactory = entityFactory;
        }

        public Task<IFileFormat> Detect(IAnalysisContext analysisContext)
        {
            var formattedFileName = Path.GetFileName(analysisContext.FilePath).PadRight(35);

            IFileFormat fileFormat = _entityFactory.CreateFileFormat(
                _referentiel,
                string.Empty,
                null,
                null);

            try
            {
                if (IsPackage(analysisContext.FilePath))
                {
                    fileFormat.Label = "Windows App Package";
                    return Task.FromResult(fileFormat);
                }

                if (IsBundle(analysisContext.FilePath))
                {
                    fileFormat.Label = "Windows App Bundle";
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"{formattedFileName}\tWindowsAppPackage error: {ex.Message}");
            }

            return Task.FromResult(fileFormat);
        }

        private bool IsPackage(string filePath)
        {
            string formattedFileName = Path.GetFileName(filePath).PadRight(35);

            foreach (var entry in _packageEntries)
            {
                var isEntryExistsResult = SevenZipService.EntryExists(filePath, entry.EntryPath);
                if (!isEntryExistsResult.IsSuccess)
                {
                    _logger.Log(LogLevel.Debug, $"{formattedFileName}\t{isEntryExistsResult.Error.Code} {isEntryExistsResult.Error.Message}");
                    return false;
                }

                if (!isEntryExistsResult.Value)
                {
                    _logger.Log(LogLevel.Debug, $"{formattedFileName}\tMissing entry: {entry.EntryPath}");
                    return false;
                }

                Result<string> entryContentResult = SevenZipService.ExtractEntryToString(filePath, entry.EntryPath);
                if (!entryContentResult.IsSuccess)
                {
                    _logger.Log(LogLevel.Debug, $"{formattedFileName}\t{entryContentResult.Error.Code} {entryContentResult.Error.Message}");
                    return false;
                }

                if (!entry.Validator(entryContentResult.Value))
                {
                    _logger.Log(LogLevel.Debug, $"{formattedFileName}\tValidation failed: {entry.EntryPath}");
                    return false;
                }
            }

            return true;
        }

        private bool IsBundle(string filePath)
        {
            string formattedFileName = Path.GetFileName(filePath).PadRight(35);

            foreach (var entry in _bundleEntries)
            {
                var isEntryExistsResult = SevenZipService.EntryExists(filePath, entry.EntryPath);
                if (!isEntryExistsResult.IsSuccess)
                {
                    _logger.Log(LogLevel.Debug, $"{formattedFileName}\t{isEntryExistsResult.Error.Code} {isEntryExistsResult.Error.Message}");
                    return false;
                }

                if (!isEntryExistsResult.Value)
                {
                    _logger.Log(LogLevel.Debug, $"{formattedFileName}\tMissing bundle entry: {entry.EntryPath}");
                    return false;
                }

                Result<string> entryContentResult = SevenZipService.ExtractEntryToString(filePath, entry.EntryPath);
                if (!entryContentResult.IsSuccess)
                {
                    _logger.Log(LogLevel.Debug, $"{formattedFileName}\t{entryContentResult.Error.Code} {entryContentResult.Error.Message}");
                    return false;
                }

                if (!entry.Validator(entryContentResult.Value))
                {
                    _logger.Log(LogLevel.Debug, $"{formattedFileName}\tBundle validation failed: {entry.EntryPath}");
                    return false;
                }
            }

            return true;
        }

        #region XSD location dictionary

        private Dictionary<string, List<List<string>>> _xsdPathsByEntry = new(StringComparer.OrdinalIgnoreCase)
        {
            ["[Content_Types].xml"] =
            [
                [@"Xsd\[Content_Types]\opc-contentTypes.xsd"]
            ],

            ["AppxBlockMap.xml"] =
            [
                [@"Xsd\BlockMap\BlockMapSchema.xsd"],
                [@"Xsd\BlockMap\BlockMapSchema.xsd", @"Xsd\BlockMap\BlockMapSchema2015.xsd"],
                [@"Xsd\BlockMap\BlockMapSchema2017.xsd"]
            ],

            ["AppxManifest.xml"] =
                [
                    [
                        @"Xsd\Manifest\2015\AppxManifestTypes.xsd", @"Xsd\Manifest\2015\AppxPhoneManifestSchema2014.xsd", @"Xsd\Manifest\2015\ComManifestSchema.xsd", @"Xsd\Manifest\2015\DesktopManifestSchema.xsd", @"Xsd\Manifest\2015\FoundationManifestSchema.xsd", @"Xsd\Manifest\2015\FoundationManifestSchema_v2.xsd", @"Xsd\Manifest\2015\HolographicManifestSchema.xsd", @"Xsd\Manifest\2015\IotManifestSchema.xsd", @"Xsd\Manifest\2015\MobileManifestSchema.xsd", @"Xsd\Manifest\2015\RestrictedCapabilitiesManifestSchema.xsd", @"Xsd\Manifest\2015\RestrictedCapabilitiesManifestSchema_v2.xsd", @"Xsd\Manifest\2015\ServerManifestSchema.xsd", @"Xsd\Manifest\2015\UapManifestSchema.xsd", @"Xsd\Manifest\2015\UapManifestSchema_v2.xsd", @"Xsd\Manifest\2015\UapManifestSchema_v3.xsd", @"Xsd\Manifest\2015\WindowsCapabilitiesManifestSchema.xsd", @"Xsd\Manifest\2015\WindowsCapabilitiesManifestSchema_v2.xsd", @"Xsd\Manifest\2015\XboxManifestSchema.xsd",
                        @"Xsd\Manifest\2016\DesktopManifestSchema_v2.xsd", @"Xsd\Manifest\2016\RestrictedCapabilitiesManifestSchema_v3.xsd", @"Xsd\Manifest\2016\UapManifestSchema_v4.xsd", @"Xsd\Manifest\2016\WindowsCapabilitiesManifestSchema_v3.xsd",
                        @"Xsd\Manifest\2017\ComManifestSchema_v2.xsd", @"Xsd\Manifest\2017\DesktopManifestSchema_v3.xsd", @"Xsd\Manifest\2017\DesktopManifestSchema_v4.xsd", @"Xsd\Manifest\2017\IotManifestSchema_v2.xsd", @"Xsd\Manifest\2017\RestrictedCapabilitiesManifestSchema_v4.xsd", @"Xsd\Manifest\2017\UapManifestSchema_v5.xsd", @"Xsd\Manifest\2017\UapManifestSchema_v6.xsd",
                        @"Xsd\Manifest\2018\DesktopManifestSchema_v5.xsd", @"Xsd\Manifest\2018\DesktopManifestSchema_v6.xsd", @"Xsd\Manifest\2018\RestrictedCapabilitiesManifestSchema_v5.xsd", @"Xsd\Manifest\2018\RestrictedCapabilitiesManifestSchema_v6.xsd", @"Xsd\Manifest\2018\UapManifestSchema_v7.xsd", @"Xsd\Manifest\2018\UapManifestSchema_v8.xsd",
                        @"Xsd\Manifest\2019\CloudFilesManifestSchema.xsd", @"Xsd\Manifest\2019\ComManifestSchema_v3.xsd", @"Xsd\Manifest\2019\PreviewManifestSchema_MsixAppCompatSupport.xsd", @"Xsd\Manifest\2019\UapManifestSchema_v10.xsd", @"Xsd\Manifest\2019\UapManifestSchema_v11.xsd",
                        @"Xsd\Manifest\2020\ComManifestSchema_v4.xsd", @"Xsd\Manifest\2020\DeploymentManifestSchema.xsd", @"Xsd\Manifest\2020\DesktopManifestSchema_v7.xsd", @"Xsd\Manifest\2020\PreviewManifestSchema_MsixAppCompatSupport_v3.xsd", @"Xsd\Manifest\2020\UapManifestSchema_v12.xsd", @"Xsd\Manifest\2020\VirtualizationManifestSchema.xsd",
                        @"Xsd\Manifest\2021\DesktopManifestSchema_v8.xsd", @"Xsd\Manifest\2021\UapManifestSchema_v13.xsd"
                    ]
                ],

            ["AppxMetadata/AppxBundleManifest.xml"] =
                [
                    [@"Xsd\Manifest\2015\AppxManifestTypes.xsd", @"Xsd\Manifest\2015\BundleManifestSchema2013.xsd", @"Xsd\Manifest\2018\BundleManifestSchema2018.xsd", @"Xsd\Manifest\2019\BundleManifestSchema2019.xsd"],
                    [@"Xsd\Manifest\2015\AppxManifestTypes.xsd", @"Xsd\Manifest\2015\BundleManifestSchema2014.xsd", @"Xsd\Manifest\2018\BundleManifestSchema2018.xsd", @"Xsd\Manifest\2019\BundleManifestSchema2019.xsd"],
                    [@"Xsd\Manifest\2015\AppxManifestTypes.xsd", @"Xsd\Manifest\2016\BundleManifestSchema2016.xsd", @"Xsd\Manifest\2018\BundleManifestSchema2018.xsd", @"Xsd\Manifest\2019\BundleManifestSchema2019.xsd"],
                    [@"Xsd\Manifest\2015\AppxManifestTypes.xsd", @"Xsd\Manifest\2017\BundleManifestSchema2017.xsd", @"Xsd\Manifest\2018\BundleManifestSchema2018.xsd", @"Xsd\Manifest\2019\BundleManifestSchema2019.xsd"],
                    [@"Xsd\Manifest\2015\AppxManifestTypes.xsd", @"Xsd\Manifest\2018\BundleManifestSchema2018.xsd", @"Xsd\Manifest\2019\BundleManifestSchema2019.xsd"]
                ]
        };

        #endregion

        #region minimum check

        private static bool IsContentTypes(string xmlContent, string manifestPath, string manifestContentType)
        {
            if (string.IsNullOrWhiteSpace(xmlContent))
                return false;

            try
            {
                XNamespace ns = "http://schemas.openxmlformats.org/package/2006/content-types";
                XDocument document = XDocument.Parse(xmlContent);
                XElement? root = document.Root;

                if (root?.Name != ns + "Types")
                    return false;

                bool hasBlockMap = false;

                foreach (XElement element in root.Elements(ns + "Override"))
                {
                    string? partName = (string?)element.Attribute("PartName");
                    string? contentType = (string?)element.Attribute("ContentType");

                    bool isBlockMapPath = string.Equals(partName, "/AppxBlockMap.xml", StringComparison.OrdinalIgnoreCase);
                    bool isBlockMapContentType = string.Equals(contentType, "application/vnd.ms-appx.blockmap+xml", StringComparison.OrdinalIgnoreCase);

                    if (isBlockMapPath && isBlockMapContentType)
                    {
                        hasBlockMap = true;
                        break;
                    }
                }

                bool hasManifest = false;

                foreach (XElement element in root.Elements(ns + "Default"))
                {
                    string? extension = (string?)element.Attribute("Extension");
                    string? contentType = (string?)element.Attribute("ContentType");

                    bool isXmlExtension = string.Equals(extension, "xml", StringComparison.OrdinalIgnoreCase);
                    bool isManifestContentType = string.Equals(contentType, manifestContentType, StringComparison.OrdinalIgnoreCase);

                    if (isXmlExtension && isManifestContentType)
                    {
                        hasManifest = true;
                        break;
                    }
                }

                if (!hasManifest)
                {
                    foreach (XElement element in root.Elements(ns + "Override"))
                    {
                        string? partName = (string?)element.Attribute("PartName");
                        string? contentType = (string?)element.Attribute("ContentType");

                        bool isManifestPath = string.Equals(partName, manifestPath, StringComparison.OrdinalIgnoreCase);
                        bool isManifestContentType = string.Equals(contentType, manifestContentType, StringComparison.OrdinalIgnoreCase);

                        if (isManifestPath && isManifestContentType)
                        {
                            hasManifest = true;
                            break;
                        }
                    }
                }

                return hasBlockMap && hasManifest;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsBlockMap(string xmlContent)
        {
            if (string.IsNullOrWhiteSpace(xmlContent))
                return false;

            try
            {
                XDocument document = XDocument.Parse(xmlContent);
                XElement? root = document.Root;

                if (root?.Name.LocalName != "BlockMap")
                    return false;

                string namespaceName = root.Name.NamespaceName;

                if (!namespaceName.StartsWith("http://schemas.microsoft.com/appx/", StringComparison.OrdinalIgnoreCase) ||
                    !namespaceName.EndsWith("/blockmap", StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!string.Equals((string?)root.Attribute("HashMethod"), "http://www.w3.org/2001/04/xmlenc#sha256", StringComparison.OrdinalIgnoreCase))
                    return false;

                XNamespace ns = root.Name.Namespace;
                List<XElement> files = root.Elements(ns + "File").ToList();

                if (files.Count == 0)
                    return false;

                foreach (XElement file in files)
                {
                    if (string.IsNullOrWhiteSpace((string?)file.Attribute("Name")))
                        return false;

                    if (!ulong.TryParse((string?)file.Attribute("Size"), out ulong fileSize))
                        return false;

                    if (file.Attribute("LfhSize") is not null && !uint.TryParse((string?)file.Attribute("LfhSize"), out _))
                        return false;

                    List<XElement> blocks = file.Elements(ns + "Block").ToList();

                    if (fileSize > 0 && blocks.Count == 0)
                        return false;

                    foreach (XElement block in blocks)
                    {
                        string? hash = (string?)block.Attribute("Hash");

                        if (string.IsNullOrWhiteSpace(hash) || Convert.FromBase64String(hash).Length != 32)
                            return false;

                        if (block.Attribute("Size") is not null && !uint.TryParse((string?)block.Attribute("Size"), out _))
                            return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsManifest(string xmlContent, string rootName)
        {
            if (string.IsNullOrWhiteSpace(xmlContent))
                return false;

            try
            {
                XDocument document = XDocument.Parse(xmlContent);
                XElement? root = document.Root;

                if (root?.Name.LocalName != rootName)
                    return false;

                string namespaceName = root.Name.NamespaceName;
                XNamespace ns = root.Name.Namespace;
                XElement? identity = root.Element(ns + "Identity");

                if (identity is null ||
                    string.IsNullOrWhiteSpace((string?)identity.Attribute("Name")) ||
                    string.IsNullOrWhiteSpace((string?)identity.Attribute("Publisher")) ||
                    string.IsNullOrWhiteSpace((string?)identity.Attribute("Version")))
                    return false;

                return rootName switch
                {
                    "Package" =>
                        string.Equals(namespaceName, "http://schemas.microsoft.com/appx/2010/manifest", StringComparison.OrdinalIgnoreCase) ||
                        namespaceName.StartsWith("http://schemas.microsoft.com/appx/manifest/", StringComparison.OrdinalIgnoreCase),

                    "Bundle" =>
                        namespaceName.StartsWith("http://schemas.microsoft.com/appx/", StringComparison.OrdinalIgnoreCase) &&
                        namespaceName.EndsWith("/bundle", StringComparison.OrdinalIgnoreCase) &&
                        root.Element(ns + "Packages") is not null,

                    _ => false
                };
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }

}

 