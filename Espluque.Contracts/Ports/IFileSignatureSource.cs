using Util;

namespace Espluque.Contracts.Ports
{
    public interface IFileSignatureSource
    {
        Task<Result<bool>> ImportFileExtensionFromXmlAsync(string xmlContent);
    }
}