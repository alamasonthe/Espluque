using Util;

namespace Espluque.Contracts.Ports;

public interface IPronomRepository
{
    Task<Result<List<KeyValuePair<string, string>>?>> GetInfosFromExtensionAsync(string extension);

    Task<Result<List<int>>> ListInternalSignatureIdsAsync();

    Task<Result<List<KeyValuePair<string, string>>?>> GetInfosFromInternalSignatureAsync(int internalSignatureId);
}