using Util;

namespace Espluque.Contracts.Ports;

public interface IDyneCategoryExtensionRepository
{
    Task<Result<bool>> InsertAsync(string extension, string category);

    Task<Result<int>> CountAsync();
}