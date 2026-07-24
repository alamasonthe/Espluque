using System.Reflection;

namespace Espluque.Contracts.ModuleInterfaces
{
    public interface ICatalogEntry
    {
        Assembly? Assembly { get; set; }
        string AssemblyPath { get; set; }
        string ClassName { get; set; }
        string InterfaceType { get; set; }
        string Label { get; set; }
        List<string> Tags { get; set; }

        string ModuleName { get; set; }
        string ModuleVersion { get; set; }
    }
}