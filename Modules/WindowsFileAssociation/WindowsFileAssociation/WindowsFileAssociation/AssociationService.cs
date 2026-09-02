using Microsoft.Win32;

namespace WindowsFileAssociation
{
    public class AssociationService
    {
        public static (string TypeLabel, string? ContentType)? GetFileTypeFromExtension(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(ext))
                return null;

            using RegistryKey? extTypeKey = Registry.ClassesRoot.OpenSubKey(ext, false);

            if (extTypeKey == null)
                return null;

            string? typeLabel = extTypeKey.GetValue(string.Empty) as string;

            if (string.IsNullOrWhiteSpace(typeLabel))
                return null;

            string? contentType = extTypeKey.GetValue("Content Type") as string;

            return (typeLabel, contentType);
        }
    }
}