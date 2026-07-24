using Microsoft.Extensions.Configuration;
using Util;
using Microsoft.Extensions.Logging;

namespace RegViewer
{
    public class RegService
    {
        private readonly Espluque.Contracts.Ports.ILogger _logger;

        public RegService(Espluque.Contracts.Ports.ILogger logger)
        {
            _logger = logger;
        }

        public async Task<Result<IConfiguration>> Load(string filePath)
        {
            var loadResult = await Viewer.Load(filePath);

            if (!loadResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"REG_LOAD_ERROR: {loadResult.Error?.Code} - {loadResult.Error?.Message}");
            }
            return loadResult;
        }

        public async Task<Result<TreeNode<List<KeyValuePair<string, string>>>>> GetRegistryKeyValueTree(string filePath)
        {
            Result<TreeNode<List<KeyValuePair<string, string>>>> treeResult = await Viewer.GetRegistryKeyValueTree(filePath);

            if (!treeResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"Reg registry key value tree failed: {treeResult.Error?.Code} - {treeResult.Error?.Message}");
            }

            return treeResult;
        }
    }
}
