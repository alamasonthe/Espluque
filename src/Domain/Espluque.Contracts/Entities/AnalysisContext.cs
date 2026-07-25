using Espluque.Contracts.Interfaces;

namespace Espluque.Contracts.Entities
{
    public class AnalysisContext
    {
        public string FilePath { get; set; } = string.Empty;
        public IFileFormat? CurrentFileFormat { get; set; }
        public List<IFileFormat> FileFormatHistory { get; set; } = [];
        public string TempFolderPath { get; set; } = string.Empty;
    }
}
