using Util;

namespace Espluque.Contracts.Ports;

public interface IDyneCategoryRepository
{
    Task<Result<bool>> InsertAsync(string category);

    Task<Result<int>> CountAsync();
}