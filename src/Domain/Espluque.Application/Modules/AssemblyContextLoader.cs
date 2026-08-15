using System.Reflection;
using System.Runtime.Loader;

namespace Espluque.Application.Modules
{
    public class AssemblyContextLoader : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;
        private readonly HashSet<string> _sharedAssemblyNames;

        public AssemblyContextLoader(
            string moduleAssemblyPath,
            IEnumerable<string>? sharedAssemblyNames = null)
            : base(isCollectible: false)
        {
            _resolver = new AssemblyDependencyResolver(moduleAssemblyPath);

            _sharedAssemblyNames = new HashSet<string>(
                sharedAssemblyNames ??
                [
                    "Contracts",
                    "Espluque.ModuleCommons"
                ],
                StringComparer.OrdinalIgnoreCase);
        }
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (assemblyName.Name is not null && _sharedAssemblyNames.Contains(assemblyName.Name))
            {
                return null;
            }

            string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);

            if (assemblyPath is null)
            {
                return null;
            }

            return LoadFromAssemblyPath(assemblyPath);
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);

            if (libraryPath is null)
            {
                return IntPtr.Zero;
            }

            return LoadUnmanagedDllFromPath(libraryPath);
        }

    }
}