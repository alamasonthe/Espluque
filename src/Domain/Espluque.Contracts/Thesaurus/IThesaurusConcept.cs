namespace Espluque.Contracts.Thesaurus
{
    public interface IThesaurusConcept
    {
        List<IThesaurusConcept> Children { get; set; }
        int? Id { get; set; }
        List<IThesaurusConcept> Parents { get; set; }
        List<IThesaurusTerm> Terms { get; set; }
    }
}