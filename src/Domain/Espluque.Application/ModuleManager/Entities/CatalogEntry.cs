using System.Reflection;
using Espluque.Contracts.ModuleInterfaces;

namespace Espluque.Application.ModuleManager.Entities
{
    public class CatalogEntry : ICatalogEntry
    {
        public required string InterfaceType { get; set; }

        public required string Label { get; set; }

        public required string ClassName { get; set; }

        public required List<string> Tags { get; set; }

        public required string AssemblyPath { get; set; }

        public Assembly? Assembly { get; set; }

        public string ModuleName { get; set; }

        public string ModuleVersion { get; set; }
    }
}