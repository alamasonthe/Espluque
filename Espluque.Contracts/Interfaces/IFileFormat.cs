namespace Espluque.Contracts.Interfaces
{
    public interface IFileFormat
    {
        string Filepath { get; set; }
        string? MIMEType { get; set; }
        string Type { get; set; }
        string? Version { get; set; }
    }
}