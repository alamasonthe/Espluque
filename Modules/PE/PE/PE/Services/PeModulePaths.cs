namespace PE.Services
{
    internal static class PeModulePaths
    {
        public static string DatabaseFilePath { get; } =
            Path.Combine( Path.GetDirectoryName(typeof(PeModulePaths).Assembly.Location)!, "pe.db");
    }
}
