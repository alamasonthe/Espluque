using Espluque.Contracts.Interfaces;

namespace Espluque.Application.Entities
{
    public class Factory : IEntityFactory
    {
        public IFileFormat CreateFileFormat(
            string filepath,
            string type,
            string? version,
            string? mimeType)
        {
            return new FileFormat
            {
                Filepath = filepath ?? string.Empty,
                Type = type ?? string.Empty,
                Version = version,
                MIMEType = mimeType
            };
        }
    }
}
