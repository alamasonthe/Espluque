using Espluque.Contracts.CrossCutting;
using Espluque.Contracts.Thesaurus;
using Espluquer.Adapters;
using Espluquer.Entities;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Espluquer.UserControls.Shell
{

    public partial class ConceptSearchUC : UserControl
    {
        private readonly IThesaurusService _thesaurusService;
        private readonly ISearchService _searchService;
        private readonly IEntityFactory _entityFactory;

        public static readonly DependencyProperty PlaceholderTextProperty = DependencyProperty.Register(
                nameof(PlaceholderText),
                typeof(string),
                typeof(ConceptSearchUC),
                new PropertyMetadata(string.Empty));

        public string PlaceholderText
        {
            get => (string)GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        private ObservableCollection<KeyValuePair<ConceptDto, string>> _concepts = [];
        public event Action<ConceptDto>? ConceptSelected;

        private bool _ignoreSearchTextChanged;

        public ConceptSearchUC(IThesaurusService thesaurusService, ISearchService searchService, IEntityFactory entityFactory)
        {
            InitializeComponent();
            _thesaurusService = thesaurusService;
            _searchService = searchService;
            _entityFactory = entityFactory;

            ConceptList.ItemsSource = _concepts;
        }

        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_ignoreSearchTextChanged) return;
            await Search();
            ConceptPopup.IsOpen = _concepts.Count > 0;
        }

        private void ConceptList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ConceptList.SelectedItem is not KeyValuePair<ConceptDto, string> selectedResult)
                return;

            _ignoreSearchTextChanged = true;

            ConceptSelected?.Invoke(selectedResult.Key);

            SearchTextBox.Text = selectedResult.Key.Term;
            SearchTextBox.CaretIndex = SearchTextBox.Text.Length;
            ConceptPopup.IsOpen = false;

            _ignoreSearchTextChanged = false;
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

            if (string.IsNullOrWhiteSpace(searchedTerm) || searchedTerm.Length < 3)
            {
                _concepts.Clear();
                ConceptPopup.IsOpen = false;
                return;
            }
        }

        public void Clear()
        {
            _ignoreSearchTextChanged = true;

            SearchTextBox.Clear();
            _concepts.Clear();
            ConceptPopup.IsOpen = false;

            _ignoreSearchTextChanged = false;
        }
    }
}
