using Espluque.Contracts.Interfaces;

namespace Espluque.Contracts.Entities
{
    public class AnalysisContext
    {
        public string? StartingTag { get; set; }
        public string? FilePath { get; set; }
        public IFileFormat? CurrentFileFormat { get; set; }
        public List<IFileFormat> FileFormatHistory { get; set; } = [];
        public List<string> TagHistory { get; set; } = [];
        public string? TempFolderPath { get; set; }
    }
}
