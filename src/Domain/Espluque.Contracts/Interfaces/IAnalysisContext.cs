using Espluque.Contracts.DetectionResult;

namespace Espluque.Contracts.Interfaces
{
    public interface IAnalysisContext
    {
        List<IAssertion> Assertions { get; set; }
        IFileFormat? CurrentFileFormat { get; set; }
        List<IFileFormat> FileFormatHistory { get; set; }
        string? FilePath { get; set; }
        List<IGrabberResult> ObservedData { get; set; }
        string? StartingTag { get; set; }
        List<string> TagHistory { get; set; }
        string? TempFolderPath { get; set; }
    }
}