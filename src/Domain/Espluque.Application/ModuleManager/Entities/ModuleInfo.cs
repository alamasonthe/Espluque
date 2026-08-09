using Espluque.Contracts.ModuleInterfaces;
using System.Text.Json.Serialization;

namespace Espluque.Application.ModuleManager.Entities;

public class ModuleInfo : IModuleInfo
{
    [JsonIgnore]
    public string FilePath { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Assembly { get; set; } = string.Empty;
    public List<IModuleContributionInfo> Contributions { get; set; } = [];
}