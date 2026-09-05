using System.Diagnostics;

namespace PE.Services
{
    internal class FileSystemService
    {
        /// <summary>
        /// Provides file-system-level information about a Portable Executable file
        /// using metadata exposed by the operating system.
        /// </summary>
        /// <remarks>
        /// This service does not parse the PE binary structure directly.
        /// It exposes version-resource metadata and file identification information.
        /// </remarks>
        public List<KeyValuePair<string, string>> GetFileInfos(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename) || !File.Exists(filename))
            {
                return new();
            }

            FileVersionInfo info = FileVersionInfo.GetVersionInfo(filename);

            string fileVersion = $"{info.FileMajorPart}.{info.FileMinorPart}.{info.FileBuildPart}.{info.FilePrivatePart}";
            string productVersion = $"{info.ProductMajorPart}.{info.ProductMinorPart}.{info.ProductBuildPart}.{info.ProductPrivatePart}";

            return new()
            {
                new("Name", Path.GetFileName(filename)),
                new("ProductName", info.ProductName ?? string.Empty),
                new("FileVersion", info.FileVersion ?? string.Empty),
                new("FileVersionNumeric", fileVersion),
                new("Comments", info.Comments ?? string.Empty),
                new("CompanyName", info.CompanyName ?? string.Empty),
                new("FileDescription", info.FileDescription ?? string.Empty),
                new("InternalName", info.InternalName ?? string.Empty),
                new("IsDebug", info.IsDebug.ToString()),
                new("IsPatched", info.IsPatched.ToString()),
                new("IsPreRelease", info.IsPreRelease.ToString()),
                new("Language", info.Language ?? string.Empty),
                new("LegalCopyright", info.LegalCopyright ?? string.Empty),
                new("LegalTrademarks", info.LegalTrademarks ?? string.Empty),
                new("OriginalFilename", info.OriginalFilename ?? string.Empty),
                new("ProductVersion", info.ProductVersion ?? string.Empty),
                new("ProductVersionNumeric", productVersion),
                new("IsPrivateBuild", info.IsPrivateBuild.ToString()),
                new("PrivateBuild", info.PrivateBuild ?? string.Empty),
                new("IsSpecialBuild", info.IsSpecialBuild.ToString()),
                new("SpecialBuild", info.SpecialBuild ?? string.Empty)
            };
        }
    }
}
