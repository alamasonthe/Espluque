namespace Espluque.Contracts.Interfaces
{
    public interface IThesaurusTerm
    {
        bool IsPrefered { get; set; }
        string? NormalizedTerm { get; set; }
        string? ReferenceName { get; set; }
        string? Term { get; set; }
    }
}