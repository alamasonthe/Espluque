using Espluque.Contracts.Interfaces;
using Espluque.Contracts.Ports;
using Espluquer.Adapters;
using Espluquer.Entities;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace Espluquer.UserControls.Debug
{

    public partial class ConceptSearchDebugUC : UserControl
    {
        private readonly IThesaurusService _thesaurusService;
        private readonly ISearchService _searchService;
        private readonly IEntityFactory _entityFactory;

        private ObservableCollection<KeyValuePair<ConceptDto, string>> _concepts = [];

        public ConceptSearchDebugUC(IThesaurusService thesaurusService, ISearchService searchService, IEntityFactory entityFactory)
        {
            InitializeComponent();
            _thesaurusService = thesaurusService;
            _searchService = searchService;
            _entityFactory = entityFactory;

            ConceptList.ItemsSource = _concepts;
        }

        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            await Search();
        }

        private async Task Search()
        {
            string searchedTerm = SearchTextBox.Text;

            if (string.IsNullOrWhiteSpace(searchedTerm) || searchedTerm.Length < 3)
            {
                _concepts.Clear();
                return;
            }

            List<KeyValuePair<int, string>> searchResults = _searchService.Search(searchedTerm, 100);

            _concepts.Clear();

            foreach (var result in searchResults)
            {
                var concept = await _thesaurusService.GetConceptById(result.Key);

                if (concept != null)
                {
                    ConceptDto conceptDto = ConceptAdapter.FromDomain(concept, _entityFactory);
                    _concepts.Add(new KeyValuePair<ConceptDto, string>(conceptDto, result.Value));
                }
            }
        }
    }
}
