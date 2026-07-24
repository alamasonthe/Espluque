using Espluque.Contracts;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Espluquer.Services
{
    internal class SqliteConfiguration
    {
        private bool _isConfigured;
        private static IntPtr _sqliteHandle;

        internal void Configure()
        {
            if (_isConfigured)
            {
                return;
            }

            _isConfigured = true;

            Assembly providerAssembly = Assembly.Load("SQLitePCLRaw.provider.e_sqlite3");

            NativeLibrary.SetDllImportResolver(providerAssembly, ResolveNativeLibrary);
        }

        private static IntPtr ResolveNativeLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (string.Equals(libraryName, "e_sqlite3", StringComparison.OrdinalIgnoreCase))
            {
                if (_sqliteHandle == IntPtr.Zero)
                {
                    _sqliteHandle = NativeLibrary.Load(RuntimePaths.SqliteLibraryFilePath);
                }

                return _sqliteHandle;
            }

            return IntPtr.Zero;
        }
    }
}