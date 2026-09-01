using Util;
using System.IO;

namespace WindowsInstaller.Files
{
    internal class FileService
    {
        private readonly WindowsInstallerService _windowsInstallerService;

        public FileService(WindowsInstallerService windowsInstallerService)
        {
            _windowsInstallerService = windowsInstallerService;
        }

        public TreeNode<MsiDirectoryItem>? GetTree(string filename)
        {
            var fileTable = _windowsInstallerService.GetTableData(filename, "File");
            var componentTable = _windowsInstallerService.GetTableData(filename, "Component");
            var directoryTable = _windowsInstallerService.GetTableData(filename, "Directory");

            if (fileTable == null ||
                componentTable == null ||
                directoryTable == null)
            {
                return null;
            }

            int directoryKeyColumn = directoryTable.Value.Columns.IndexOf("Directory");
            int directoryParentColumn = directoryTable.Value.Columns.IndexOf("Directory_Parent");
            int defaultDirColumn = directoryTable.Value.Columns.IndexOf("DefaultDir");

            Dictionary<string, (string Parent, string TargetName, string SourceName)> directories =
                directoryTable.Value.Rows.ToDictionary(
                    row => row[directoryKeyColumn],
                    row =>
                    {
                        var names = GetDirectoryNames(row[defaultDirColumn]);

                        return (
                            Parent: row[directoryParentColumn],
                            TargetName: names.TargetName,
                            SourceName: names.SourceName);
                    });

            List<MsiDirectoryItem> directoryItems =
                directoryTable.Value.Rows
                    .Select(row =>
                    {
                        string directoryKey = row[directoryKeyColumn];
                        var names = GetDirectoryNames(row[defaultDirColumn]);

                        return new MsiDirectoryItem
                        {
                            DirectoryKey = directoryKey,
                            ParentDirectoryKey = row[directoryParentColumn],
                            TargetName = names.TargetName,
                            SourceName = names.SourceName
                        };
                    })
                    .ToList();

            List<MsiFileItem> files = BuildFiles(
                fileTable.Value,
                componentTable.Value,
                directories);

            PopulateDirectoryFiles(directoryItems, files);

            var treeItems = directoryItems.Select(directory => (
                Path: BuildDirectoryKeyPath(directory.DirectoryKey, directories),
                IsLeaf: false,
                Data: directory));

            return TreeBuilder.Build(
                treeItems,
                ["\\"],
                Path.GetFileName(filename));
        }

        private static List<MsiFileItem> BuildFiles(
            (List<string> Columns, List<List<string>> Rows) fileTable,
            (List<string> Columns, List<List<string>> Rows) componentTable,
            Dictionary<string, (string Parent, string TargetName, string SourceName)> directories)
        {
            int fileKeyColumn = fileTable.Columns.IndexOf("File");
            int componentColumn = fileTable.Columns.IndexOf("Component_");
            int fileNameColumn = fileTable.Columns.IndexOf("FileName");
            int fileSizeColumn = fileTable.Columns.IndexOf("FileSize");
            int attributesColumn = fileTable.Columns.IndexOf("Attributes");
            int sequenceColumn = fileTable.Columns.IndexOf("Sequence");
            int versionColumn = fileTable.Columns.IndexOf("Version");

            int componentKeyColumn = componentTable.Columns.IndexOf("Component");
            int componentDirectoryColumn = componentTable.Columns.IndexOf("Directory_");

            Dictionary<string, string> componentDirectories =
                componentTable.Rows.ToDictionary(
                    row => row[componentKeyColumn],
                    row => row[componentDirectoryColumn]);

            List<MsiFileItem> files = [];

            foreach (List<string> row in fileTable.Rows)
            {
                string fileKey = row[fileKeyColumn];
                string componentKey = row[componentColumn];
                string targetName = GetLongName(row[fileNameColumn]);

                if (!componentDirectories.TryGetValue(componentKey, out string? directoryKey))
                {
                    continue;
                }

                string targetDirectoryPath = BuildDirectoryPath(directoryKey, directories, source: false);

                string sourceDirectoryPath = BuildDirectoryPath(directoryKey, directories, source: true);

                files.Add(new MsiFileItem
                {
                    FileKey = fileKey,
                    TargetName = targetName,

                    TargetPath = string.IsNullOrWhiteSpace(targetDirectoryPath)
                        ? targetName
                        : $"{targetDirectoryPath}\\{targetName}",

                    SourcePath = string.IsNullOrWhiteSpace(sourceDirectoryPath)
                        ? targetName
                        : $"{sourceDirectoryPath}\\{targetName}",

                    ComponentKey = componentKey,
                    DirectoryKey = directoryKey,

                    FileSize = long.TryParse(
                        row[fileSizeColumn],
                        out long fileSize)
                            ? fileSize
                            : 0,

                    Attributes = attributesColumn >= 0 &&
                                 int.TryParse(
                                     row[attributesColumn],
                                     out int attributes)
                                        ? attributes
                                        : 0,

                    FileVersion = versionColumn >= 0
                        ? row[versionColumn]
                        : string.Empty,

                    Sequence = int.TryParse(
                        row[sequenceColumn],
                        out int sequence)
                            ? sequence
                            : 0
                });
            }

            return files;
        }

        private static void PopulateDirectoryFiles(
            IEnumerable<MsiDirectoryItem> directories,
            IEnumerable<MsiFileItem> files)
        {
            Dictionary<string, MsiDirectoryItem> directoriesByKey =
                directories.ToDictionary(
                    directory => directory.DirectoryKey,
                    StringComparer.Ordinal);

            foreach (MsiFileItem file in files)
            {
                if (directoriesByKey.TryGetValue(
                    file.DirectoryKey,
                    out MsiDirectoryItem? directory))
                {
                    directory.Files.Add(file);
                }
            }
        }

        private static string BuildDirectoryKeyPath(
            string directoryKey,
            Dictionary<string, (string Parent, string TargetName, string SourceName)> directories)
        {
            List<string> parts = [];

            string? currentKey = directoryKey;

            while (!string.IsNullOrWhiteSpace(currentKey) &&
                   directories.TryGetValue(currentKey, out var directory))
            {
                parts.Add(currentKey);
                currentKey = directory.Parent;
            }

            parts.Reverse();

            return string.Join("\\", parts);
        }

        private static string BuildDirectoryPath(
    string directoryKey,
    Dictionary<string, (string Parent, string TargetName, string SourceName)> directories,
    bool source)
        {
            List<string> parts = [];

            string? currentKey = directoryKey;

            while (!string.IsNullOrWhiteSpace(currentKey) &&
                   directories.TryGetValue(currentKey, out var directory))
            {
                if (!string.Equals(
                    currentKey,
                    "TARGETDIR",
                    StringComparison.OrdinalIgnoreCase))
                {
                    string name = source
                        ? directory.SourceName
                        : directory.TargetName;

                    if (!string.IsNullOrWhiteSpace(name) &&
                        name != ".")
                    {
                        parts.Add(name);
                    }
                }

                currentKey = directory.Parent;
            }

            parts.Reverse();

            return string.Join("\\", parts);
        }

        private static (string TargetName, string SourceName) GetDirectoryNames(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return (string.Empty, string.Empty);
            }

            string[] parts = value.Split(':', 2);

            string targetName = GetLongName(parts[0]);

            string sourceName = parts.Length == 2
                ? GetLongName(parts[1])
                : targetName;

            return (targetName, sourceName);
        }

        private static string GetLongName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            int separator = value.IndexOf('|');

            return separator >= 0
                ? value[(separator + 1)..]
                : value;
        }

        public string? GetCabinet(string filename, MsiFileItem file)
        {
            if (!IsCompressed(filename, file))
                return null;

            var mediaTable = _windowsInstallerService.GetTableData(filename, "Media");

            if (mediaTable == null)
                throw new InvalidOperationException("Media table not found.");

            int lastSequenceColumn =
                mediaTable.Value.Columns.IndexOf("LastSequence");

            int cabinetColumn =
                mediaTable.Value.Columns.IndexOf("Cabinet");

            List<string>? mediaRow = mediaTable.Value.Rows
                .Where(row =>
                    int.TryParse(
                        row[lastSequenceColumn],
                        out int lastSequence) &&
                    lastSequence >= file.Sequence)
                .OrderBy(row =>
                    int.Parse(row[lastSequenceColumn]))
                .FirstOrDefault();

            if (mediaRow == null)
                throw new InvalidOperationException(
                    $"No media found for file '{file.FileKey}'.");

            string cabinet = mediaRow[cabinetColumn];

            if (string.IsNullOrWhiteSpace(cabinet))
                throw new InvalidOperationException(
                    $"File '{file.FileKey}' is compressed but has no cabinet.");

            return cabinet;
        }

        private bool IsCompressed(string filename, MsiFileItem file)
        {
            const int nonCompressedAttribute = 8192;
            const int compressedAttribute = 16384;

            if ((file.Attributes & nonCompressedAttribute) != 0)
                return false;

            if ((file.Attributes & compressedAttribute) != 0)
                return true;

            int wordCount = _windowsInstallerService.GetWordCount(filename) ?? 0;

            return (wordCount & 2) != 0;
        }
    }
}