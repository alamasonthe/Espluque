using Espluque.Contracts.Contributions.Types;
using System.IO;

namespace VlcViewer
{
    public class Preload : IManagedDependencies
    {
        public List<string> GetDependencyPaths()
        {
            string moduleRootPath = Path.GetDirectoryName(typeof(Preload).Assembly.Location)!;

            List<string> paths =
            [
                Path.Combine(moduleRootPath, "LibVLCSharp.dll"),
                Path.Combine(moduleRootPath, "LibVLCSharp.WPF.dll")
            ];

            return paths;
        }
    }
}
