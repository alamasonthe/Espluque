using Util;

namespace PronomSqlite
{
    public interface IPronomImportService
    {
        Task<bool> ImportFileExtensionFromXmlAsync(string filePath);
    }
}