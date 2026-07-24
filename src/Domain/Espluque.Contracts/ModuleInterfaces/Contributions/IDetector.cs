using Espluque.Contracts.Interfaces;

namespace Espluque.Contracts.ModuleInterfaces.Contributions
{
    public interface IDetector
    {
        Task<IFileFormat> Detect(string filePath);
    }
}
