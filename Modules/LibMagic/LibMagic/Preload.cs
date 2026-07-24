using Espluque.Contracts.ModuleInterfaces.Contributions;

namespace LibMagic
{
    public class Preload: IManagedDependencies
    {
        public List<string> GetDependencyPaths()
        {
            string moduleRootPath = Path.GetDirectoryName(typeof(Detector).Assembly.Location)!;

            List<string> paths =
            [
                Path.Combine(moduleRootPath, "Mime.dll")
            ];

            return paths;
        }
    }
}
