using Espluque.Contracts.Interfaces;
using Espluque.Contracts.Ports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Espluquer.Entities;

namespace Espluquer.UserControls.Thesaurus
{
    public partial class ReferenceUC : RefreshableUserControl
    {
        private readonly IThesaurusService? _thesaurusService;
        private readonly ILogger? _logger;

        public List<string> References { get; set; } = [];
        private List<ReferenceTermDto> _referenceTerms { get; set; } = [];
        private List<ReferenceTermDto> _alternateTerms { get; set; } = [];

        public ReferenceUC( IThesaurusService thesaurusService, ILogger logger)
        {
            _thesaurusService = thesaurusService;
            _logger = logger;

            InitializeComponent();

            ReferenceTermsItemsControl.ItemsSource = _referenceTerms;

        }

        protected override async Task RefreshAsync()
        {
            string? selectedReference = ReferencesListBox.SelectedItem as string;
            List<string> references = await _thesaurusService!.GetReferences();

            ReferencesListBox.SelectedIndex = -1;

            References.Clear();
            References.AddRange(references);
            ReferencesListBox.Items.Refresh();

            if (References.Count == 0)
            {
                ReferenceNameTextBox.Clear();

                _referenceTerms.Clear();
                _alternateTerms.Clear();

                ReferenceTermsItemsControl.Items.Refresh();
                UpdateReferenceIndicators();

                return;
            }

            string referenceToSelect = selectedReference is not null && References.Contains(selectedReference)
                ? selectedReference
                : References[0];

            ReferencesListBox.SelectedItem = referenceToSelect;
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

        private void NewReferenceTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CancelReferenceCreation();
                return;
            }

            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                ReferencesListBox.Focus();
            }
        }

        private async void NewReferenceTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            string referenceName = NewReferenceTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(referenceName))
            {
                CancelReferenceCreation();
                return;
            }

            string? existingReference = References.FirstOrDefault(reference =>
                string.Equals(reference, referenceName, StringComparison.OrdinalIgnoreCase));

            if (existingReference is not null)
            {
                ReferencesListBox.SelectedItem = existingReference;
                CancelReferenceCreation();
                return;
            }

            bool isSaved = await _thesaurusService!.SaveReference(referenceName);

            if (isSaved)
            {
                References.Add(referenceName);
                References.Sort(StringComparer.OrdinalIgnoreCase);
                ReferencesListBox.Items.Refresh();
                ReferencesListBox.SelectedItem = referenceName;
            }

            CancelReferenceCreation();
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

        private static void UpdateTermList( List<ReferenceTermDto> target, IEnumerable<IReferenceTerm> source)
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

            int preferredTermCount = _referenceTerms
                .Count(term => term.IsPreferred);

            int withoutAlternativeCount = _referenceTerms
                .Where(term => term.IsPreferred && term.ConceptId.HasValue)
                .Select(term => term.ConceptId!.Value)
                .Distinct()
                .Count(conceptId => !_alternateTerms.Any(
                    term => term.ConceptId == conceptId));

            ReferenceTermCountText.Text = termCount.ToString();
            PreferredTermCountText.Text = preferredTermCount.ToString();
            ConceptWithoutAlternativeCountText.Text = withoutAlternativeCount.ToString();

            ReferenceWarningBorder.Visibility =
                withoutAlternativeCount > 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            ReferenceWarningText.Text =
                withoutAlternativeCount > 0
                    ? $"Deleting this reference will also delete {withoutAlternativeCount} associated " +
                      $"{(withoutAlternativeCount == 1 ? "concept" : "concepts")} because " +
                      $"{(withoutAlternativeCount == 1 ? "it has" : "they have")} no alternative term."
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

        private void DeleteReferenceButton_Click(object sender, RoutedEventArgs e)
        {
            bool isDeleted = _thesaurusService!.DeleteReference(ReferenceNameTextBox.Text).GetAwaiter().GetResult();

            if (!isDeleted)
            {
                return;
            }

            List<string> references = _thesaurusService.GetReferences().GetAwaiter().GetResult();

            References.Clear();
            References.AddRange(references);

            ReferencesListBox.Items.Refresh();

            if (References.Count > 0)
            {
                ReferencesListBox.SelectedIndex = 0;
            }
        }

        #region Rename reference

        private async void ReferenceNameTextBox_KeyDown( object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            e.Handled = true;

            await RenameSelectedReferenceAsync();
        }

        private async void ReferenceNameTextBox_LostKeyboardFocus( object sender, KeyboardFocusChangedEventArgs e)
        {
            await RenameSelectedReferenceAsync();
        }

        private async Task RenameSelectedReferenceAsync()
        {
            if (ReferencesListBox.SelectedItem is not string oldName)
            {
                return;
            }

            string newName = ReferenceNameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(newName))
            {
                ReferenceNameTextBox.Text = oldName;
                return;
            }

            if (string.Equals( oldName, newName, StringComparison.Ordinal))
            {
                return;
            }

            bool isRenamed = await _thesaurusService!.RenameReference( oldName, newName);

            if (!isRenamed)
            {
                ReferenceNameTextBox.Text = oldName;
                return;
            }

            int referenceIndex = References.IndexOf(oldName);

            if (referenceIndex >= 0)
            {
                References[referenceIndex] = newName;
            }

            References.Sort(StringComparer.OrdinalIgnoreCase);

            ReferencesListBox.Items.Refresh();
            ReferencesListBox.SelectedItem = newName;
        }

        #endregion
    }
}