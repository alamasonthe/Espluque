using Util;

namespace Espluque.Contracts.Ports;

public interface IDyneExtensionRepository
{
    Task<Result<bool>> UpsertAsync(string extension, string? openClose, string? textBinary);

    Task<Result<int>> CountAsync();

    Task<Result<bool>> InsertAsync(string extension);

    Task<Result<List<KeyValuePair<string, string>>?>> GetInfosFromExtensionAsync(string extension);
}