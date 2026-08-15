namespace Espluque.Contracts.Thesaurus
{
    public interface ISearchService
    {
        void Dispose();
        Task Index(IThesaurusService thesaurusService);
        List<KeyValuePair<int, string>> Search(string searchedTerm, int maxResults);
    }
}