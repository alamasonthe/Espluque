using System.Reflection;
using Espluque.Contracts.Catalog;

namespace Espluque.Application.Catalog
{
    /// <summary>
    /// Represents an active contribution available in the runtime catalog.
    /// </summary>
    /// <remarks>
    /// InterfaceType and Tags are used to select the contribution.
    /// Assembly and ClassName identify the implementation to instantiate.
    /// ModuleName and ModuleVersion identify the module providing the contribution.
    /// </remarks>

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