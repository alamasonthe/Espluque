namespace Espluque.Contracts.Interfaces;

public interface IEntityFactory
{
    IFileFormat CreateFileFormat(string filepath, string type, string? version, string? mimeType);
}