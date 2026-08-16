using Espluque.Contracts.Contributions.Types;
using System.IO;

namespace RegViewer
{
    public class Preload: IManagedDependencies
    {
        public List<string> GetDependencyPaths()
        {
            string moduleRootPath = Path.GetDirectoryName(typeof(Preload).Assembly.Location)!;

            List<string> paths =
            [
                Path.Combine(moduleRootPath, "Microsoft.Extensions.Configuration.dll"),
                Path.Combine(moduleRootPath, "Microsoft.Extensions.Configuration.Abstractions.dll"),
                Path.Combine(moduleRootPath, "Microsoft.Extensions.Configuration.Ini.dll"),
                Path.Combine(moduleRootPath, "Microsoft.Extensions.Configuration.FileExtensions.dll"),
                Path.Combine(moduleRootPath, "Microsoft.Extensions.FileProviders.Abstractions.dll"),
                Path.Combine(moduleRootPath, "Microsoft.Extensions.FileProviders.Physical.dll"),
                Path.Combine(moduleRootPath, "Microsoft.Extensions.FileSystemGlobbing.dll"),
                Path.Combine(moduleRootPath, "Microsoft.Extensions.Primitives.dll")
            ];

            return paths;
        }
    }
}
