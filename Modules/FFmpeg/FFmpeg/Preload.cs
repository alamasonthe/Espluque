using Espluque.Contracts.ModuleInterfaces.Contributions;

namespace FFmpeg
{
    public class Preload: IManagedDependencies
    {
        public List<string> GetDependencyPaths()
        {
            string moduleRootPath = Path.GetDirectoryName(typeof(Grabber).Assembly.Location)!;

            List<string> paths =
            [
                Path.Combine(moduleRootPath, "FFmpeg.AutoGen.dll")
            ];

            return paths;
        }
    }
}
