using HeyRed.Mime;
using System.Reflection;
using System.Runtime.InteropServices;

namespace LibMagic
{
    internal static class MimeConfiguration
    {
        private static readonly string NativeDirectory =
            Path.Combine(
                Path.GetDirectoryName(typeof(MimeConfiguration).Assembly.Location)!,
                "runtimes",
                "win-x64",
                "native");

        private static IntPtr _libGnuRxHandle;

        static MimeConfiguration()
        {
            NativeLibrary.SetDllImportResolver(
                typeof(MimeGuesser).Assembly,
                ResolveNativeLibrary);

            MimeGuesser.MagicFilePath =
                Path.Combine(NativeDirectory, "magic.mgc");
        }

        internal static void Configure()
        {
        }

        private static IntPtr ResolveNativeLibrary(
            string libraryName,
            Assembly assembly,
            DllImportSearchPath? searchPath)
        {
            if (!string.Equals(
                libraryName,
                "libmagic-1",
                StringComparison.OrdinalIgnoreCase))
            {
                return IntPtr.Zero;
            }

            if (_libGnuRxHandle == IntPtr.Zero)
            {
                _libGnuRxHandle = NativeLibrary.Load(
                    Path.Combine(NativeDirectory, "libgnurx-0.dll"));
            }

            return NativeLibrary.Load(
                Path.Combine(NativeDirectory, "libmagic-1.dll"));
        }
    }
}