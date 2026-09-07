namespace PE.Services
{
    internal static class PeModulePaths
    {
        public static string DatabaseFilePath { get; } =
            Path.Combine( Path.GetDirectoryName(typeof(PeModulePaths).Assembly.Location)!, "pe.db");

        public static string CacheFilePath(string tempFolderPath)
        {
            string assemblyName = typeof(PeModulePaths).Assembly.GetName().Name!;
            return Path.Combine(tempFolderPath, $"{assemblyName}_pe_cache.json");
        }
    }
}
