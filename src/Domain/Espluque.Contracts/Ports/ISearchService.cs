using Espluque.Contracts.Interfaces;

namespace Espluque.Contracts.Ports
{
    public interface ISearchService
    {
        void Dispose();
        Task Index(IThesaurusService thesaurusService);
        List<KeyValuePair<int, string>> Search(string searchedTerm, int maxResults);
    }
}