namespace Espluque.Contracts.Interfaces
{
    public interface IFileFormat
    {
        string? Referentiel { get; set; }
        string? Label { get; set; }
        string? Version { get; set; }
        string? MIMEType { get; set; }
       
    }
}