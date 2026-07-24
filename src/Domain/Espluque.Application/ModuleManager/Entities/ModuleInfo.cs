using Espluque.Contracts.ModuleInterfaces;

namespace Espluque.Application.ModuleManager.Entities;

public class ModuleInfo : IModuleInfo
{
    public string Name { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string Assembly { get; set; } = string.Empty;

    public List<IModuleContributionInfo> Contributions { get; set; } = [];
}