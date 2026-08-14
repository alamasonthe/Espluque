namespace Espluque.Contracts.ModuleInterfaces
{
    public interface IModuleService
    {
        Task<List<IModuleInfo>> GetModuleInfoList(List<string> moduleInfoPaths);
        List<string> GetModuleInfoPaths(string modulesRootPath);
        Task<IModuleInfo?> LoadModuleInfo(string moduleInfoPath);
        Task<bool> SaveModuleInfo(IModuleInfo moduleInfo, string? filePath = null);
    }
}