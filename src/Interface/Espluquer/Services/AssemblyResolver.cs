using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Espluquer.Services
{
    internal static class AssemblyResolver
    {
        private static bool _isRegistered;

        internal static void Register()
        {
            if (_isRegistered)
            {
                return;
            }

            _isRegistered = true;

            AssemblyLoadContext.Default.Resolving += ResolveManagedAssembly;

            var moduleCommonsPath = Path.Combine( AppContext.BaseDirectory, "managed", "Espluque", "Espluque.ModuleCommons.dll");
            AssemblyLoadContext.Default.LoadFromAssemblyPath(moduleCommonsPath);
        }

        private static Assembly? ResolveManagedAssembly(
            AssemblyLoadContext context,
            AssemblyName assemblyName)
        {
            string[] probingDirectories =
            [
                Path.Combine(AppContext.BaseDirectory, "managed", "Espluque"),
                Path.Combine(AppContext.BaseDirectory, "managed", "nuGet")
            ];

            foreach (string probingDirectory in probingDirectories)
            {
                string assemblyPath = Path.Combine(
                    probingDirectory,
                    assemblyName.Name + ".dll");

                if (File.Exists(assemblyPath))
                {
                    return context.LoadFromAssemblyPath(assemblyPath);
                }
            }

            return null;
        }
    }
}