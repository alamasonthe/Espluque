namespace Espluque.Contracts
{
    public static class RuntimePaths
    {
        public static string BaseDirectory => AppContext.BaseDirectory;

        public static string SevenZipLibraryFilePath => Path.Combine(BaseDirectory, "native", "sevenzip", "7z.dll");

        public static string NativeWebView2DirectoryPath => Path.Combine(BaseDirectory, "native", "webview2");

        public static string NativeMagickDirectoryPath => Path.Combine(BaseDirectory, "native", "magick");

        public static string LibMagicLibraryFilePath => Path.Combine(BaseDirectory, "native", "mime", "libmagic-1.dll");
        public static string LibGnuRxLibraryFilePath => Path.Combine(BaseDirectory, "native", "mime", "libgnurx-0.dll");
        public static string MagicDatabaseFilePath => Path.Combine(BaseDirectory, "native", "mime", "magic.mgc");

        public static string NativeFFmpegDirectoryPath => Path.Combine(BaseDirectory, "native", "ffmpeg");
        public static string NativeLibVlcDirectoryPath => Path.Combine(BaseDirectory, "native", "libvlc", "win-x64");
        public static string SqliteLibraryFilePath => Path.Combine(BaseDirectory, "native", "sqlite", "e_sqlite3.dll");
    }
}