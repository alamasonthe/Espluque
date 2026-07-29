using Espluque.Contracts.Interfaces;
using Espluque.Contracts.Ports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Espluquer.Entities;

namespace Espluquer.UserControls.Parameters
{
    public partial class ReferenceUC : UserControl
    {
        private readonly IThesaurusService? _thesaurusService;
        private readonly ILogger? _logger;

        public List<string> References { get; } = [];
        private List<ReferenceTermDto> _referenceTerms { get; } = [];
        private List<ReferenceTermDto> _alternateTerms { get; } = [];

        public ReferenceUC( IThesaurusService thesaurusService, ILogger logger)
        {
            _thesaurusService = thesaurusService;
            _logger = logger;

            References = _thesaurusService.GetReferences().GetAwaiter().GetResult();

            InitializeComponent();

            ReferenceTermsItemsControl.ItemsSource = _referenceTerms;

            if (References.Count > 0)
            {
                ReferencesListBox.SelectedIndex = 0;
            }
        }

        private void AddReferenceButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            AddReferenceButton.Visibility = Visibility.Collapsed;
            NewReferenceTextBox.Visibility = Visibility.Visible;

            NewReferenceTextBox.Clear();
            NewReferenceTextBox.Focus();
        }

        private void NewReferenceTextBox_KeyDown( object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CancelReferenceCreation();
                return;
            }

            if (e.Key != Key.Enter)
            {
                return;
            }

            string referenceName = NewReferenceTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(referenceName))
            {
                return;
            }

            string? existingReference = References.FirstOrDefault(
                reference => string.Equals(
                    reference,
                    referenceName,
                    System.StringComparison.OrdinalIgnoreCase));

            if (existingReference is null)
            {
                References.Add(referenceName);
                References.Sort(System.StringComparer.OrdinalIgnoreCase);

                ReferencesListBox.Items.Refresh();
                ReferencesListBox.SelectedItem = referenceName;
            }
            else
            {
                ReferencesListBox.SelectedItem = existingReference;
            }

            CancelReferenceCreation();
        }

        private void NewReferenceTextBox_LostKeyboardFocus( object sender, KeyboardFocusChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewReferenceTextBox.Text))
            {
                CancelReferenceCreation();
            }
        }

        private void CancelReferenceCreation()
        {
            NewReferenceTextBox.Clear();
            NewReferenceTextBox.Visibility = Visibility.Collapsed;
            AddReferenceButton.Visibility = Visibility.Visible;
        }

        private async void ReferencesListBox_SelectionChanged( object sender, SelectionChangedEventArgs e)
        {
            if (ReferencesListBox.SelectedItem is string referenceName)
            {
                await LoadReferenceAsync(referenceName);
            }
        }

        #region Load reference

        private async Task LoadReferenceAsync(string referenceName)
        {
            ReferenceNameTextBox.Text = referenceName;

            List<IReferenceTerm> referenceTerms =
                await _thesaurusService!.GetReferenceTerms(referenceName);

            List<IReferenceTerm> alternateTerms =
                await _thesaurusService.GetAlternateTerms(referenceName);

            UpdateTermList(_referenceTerms, referenceTerms);
            UpdateTermList(_alternateTerms, alternateTerms);

            UpdateConceptTermCounts();

            ReferenceTermsItemsControl.Items.Refresh();

            UpdateReferenceIndicators();
        }

        private static void UpdateTermList(
            List<ReferenceTermDto> target,
            IEnumerable<IReferenceTerm> source)
        {
            target.Clear();

            target.AddRange(source.Select(term => new ReferenceTermDto
            {
                ConceptId = term.ConceptId,
                ReferenceName = term.ReferenceName,
                Term = term.Term,
                NormalizedTerm = term.NormalizedTerm,
                IsPreferred = term.IsPreferred,
                PreferredTerm = term.PreferredTerm
            }));
        }

        private void UpdateReferenceIndicators()
        {
            int termCount = _referenceTerms.Count;

            int conceptCount = _referenceTerms
                .Where(term => term.ConceptId.HasValue)
                .Select(term => term.ConceptId!.Value)
                .Distinct()
                .Count();

            int preferredTermCount = _referenceTerms
                .Count(term => term.IsPreferred);

            int withoutAlternativeCount = _referenceTerms
                .Where(term => term.IsPreferred && term.ConceptId.HasValue)
                .Select(term => term.ConceptId!.Value)
                .Distinct()
                .Count(conceptId => !_alternateTerms.Any(
                    term => term.ConceptId == conceptId));

            ReferenceTermCountText.Text = termCount.ToString();
            ReferenceConceptCountText.Text = conceptCount.ToString();
            PreferredTermCountText.Text = preferredTermCount.ToString();
            ConceptWithoutAlternativeCountText.Text = withoutAlternativeCount.ToString();

            ReferenceWarningBorder.Visibility =
                withoutAlternativeCount > 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            ReferenceWarningText.Text =
                withoutAlternativeCount > 0
                    ? "Deletion is not possible because one or more concepts would have no remaining term."
                    : string.Empty;
        }

        private void UpdateConceptTermCounts()
        {
            foreach (ReferenceTermDto term in _referenceTerms)
            {
                term.ConceptTermCount = term.ConceptId.HasValue
                    ? _referenceTerms.Count(candidate =>
                          candidate.ConceptId == term.ConceptId)
                      + _alternateTerms.Count(candidate =>
                          candidate.ConceptId == term.ConceptId)
                    : 0;
            }

            foreach (ReferenceTermDto term in _alternateTerms)
            {
                term.ConceptTermCount = term.ConceptId.HasValue
                    ? _referenceTerms.Count(candidate =>
                          candidate.ConceptId == term.ConceptId)
                      + _alternateTerms.Count(candidate =>
                          candidate.ConceptId == term.ConceptId)
                    : 0;
            }
        }

        #endregion
    }
}