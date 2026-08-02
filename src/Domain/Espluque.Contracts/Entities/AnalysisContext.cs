using Espluque.Contracts.DetectionResult;
using Espluque.Contracts.Interfaces;

namespace Espluque.Contracts.Entities
{
    public class AnalysisContext
    {
        public string? StartingTag { get; set; }
        public List<string> TagHistory { get; set; } = [];
        public string? FilePath { get; set; }
        public IFileFormat? CurrentFileFormat { get; set; }
        public List<IFileFormat> FileFormatHistory { get; set; } = [];
        public string? TempFolderPath { get; set; }

        public List<IGrabberResult> ObservedData { get; set; } = [];

        public List<IAssertion> Assertions { get; set; } = [];
    }
}
