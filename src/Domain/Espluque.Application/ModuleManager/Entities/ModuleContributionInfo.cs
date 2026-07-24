using Espluque.Contracts.ModuleInterfaces;

namespace Espluque.Application.ModuleManager.Entities;

public class ModuleContributionInfo : IModuleContributionInfo
{
    public string InterfaceType { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string ClassName { get; set; } = string.Empty;

    public List<string> Tags { get; set; } = new();

    public bool Active { get; set; } = true;
}