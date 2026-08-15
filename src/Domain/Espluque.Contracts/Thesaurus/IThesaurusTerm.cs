namespace Espluque.Contracts.Thesaurus
{
    public interface IThesaurusTerm
    {
        bool IsPreferred { get; set; }
        string? NormalizedTerm { get; set; }
        string? ReferenceName { get; set; }
        string? Term { get; set; }
    }
}