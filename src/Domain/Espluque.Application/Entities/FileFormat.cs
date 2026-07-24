using Espluque.Contracts.Interfaces;

namespace Espluque.Application.Entities
{
    public class FileFormat : IFileFormat
    {
        public string? Referentiel { get; set; }

        public string? Label { get; set; }

        public string? Version { get; set; }

        public string? MIMEType { get; set; }
    }
}
