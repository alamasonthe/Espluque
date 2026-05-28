using Util;

namespace Espluque.Contracts.Ports;

public interface IDyneFileSource
{
    Task<Result<bool>> ImportExtensionFromCsvAsync(string filePath);

    Task<Result<bool>> ImportExtensionFromJsonAsync(string filePath);
}