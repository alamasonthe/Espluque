using Espluque.Contracts.Interfaces;

namespace Espluque.Application.Entities
{
    public class FileFormat : IFileFormat
    {
        public string Filepath { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public string? Version { get; set; }

        public string? MIMEType { get; set; }
    }
}
