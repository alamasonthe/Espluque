using Espluque.Contracts.Contributions.Types;

namespace Magick
{
    public class Preload: IManagedDependencies
    {
        public List<string> GetDependencyPaths()
        {
            string moduleRootPath = Path.GetDirectoryName(typeof(Preload).Assembly.Location)!;

            List<string> paths =
            [
                Path.Combine(moduleRootPath, "MagickViewer.dll"),

                Path.Combine(moduleRootPath, "Magick.NET.Core.dll"),
                Path.Combine(moduleRootPath, "Magick.NET-Q16-x64.dll")
            ];

            return paths;
        }
    }
}
