using Espluque.Application.Thesaurus.Entities;
using Espluque.Contracts.Interfaces;
using Espluquer.Entities;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Espluquer.UserControls.Thesaurus
{
    public partial class ThesaurusConceptDetailsUC : UserControl
    {
        private ConceptDto _conceptDto;
        private readonly IThesaurusService _thesaurusService;

        public event EventHandler? DeleteRequested;
        public ObservableCollection<string> ReferenceNames { get; } = [];
        private List<IThesaurusTerm> _originalTerms = [];

        public ThesaurusConceptDetailsUC(ConceptDto conceptDto, IThesaurusService thesaurusService)
        {
            _conceptDto = conceptDto;
            _thesaurusService = thesaurusService;

            InitializeComponent();

            DataContext = _conceptDto;
            _conceptDto.Terms.Sort(TermsQuickSort);
            _originalTerms = CopyTerms(_conceptDto.Terms);

            Loaded += LoadReferencesAsync;
        }

        #region Title / Concept.Id - Main Term

        private void DeleteConceptButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteRequested?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Parents / Children

        private void DeleteParentRelationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
            {
                return;
            }

            if (button.DataContext is not ConceptDto parentConcept)
            {
                return;
            }

            _conceptDto.Parents = _conceptDto.Parents
                .Where(parent => !ReferenceEquals(parent, parentConcept))
                .ToList();
        }

        private void DeleteChildRelationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
            {
                return;
            }

            if (button.DataContext is not ConceptDto childConcept)
            {
                return;
            }

            _conceptDto.Children = _conceptDto.Children
                .Where(child => !ReferenceEquals(child, childConcept))
                .ToList();
        }

        #endregion

        #region Terms

        private static int TermsQuickSort(IThesaurusTerm left, IThesaurusTerm right)
        {
            int preferredComparison = right.IsPreferred.CompareTo(left.IsPreferred);

            if (preferredComparison != 0)
            {
                return preferredComparison;
            }

            bool leftIsEmpty = string.IsNullOrWhiteSpace(left.Term);
            bool rightIsEmpty = string.IsNullOrWhiteSpace(right.Term);

            if (leftIsEmpty != rightIsEmpty)
            {
                return leftIsEmpty ? 1 : -1;
            }

            return string.Compare(left.Term, right.Term, StringComparison.CurrentCultureIgnoreCase);
        }

        private static List<IThesaurusTerm> CopyTerms(List<IThesaurusTerm> terms)
        {
            List<IThesaurusTerm> copiedTerms = [];

            foreach (IThesaurusTerm term in terms)
            {
                copiedTerms.Add(new ThesaurusTerm
                {
                    Term = term.Term?.Trim() ?? string.Empty,
                    NormalizedTerm = term.Term?.Trim() ?? string.Empty,
                    ReferenceName = string.IsNullOrWhiteSpace(term.ReferenceName)
                        ? null
                        : term.ReferenceName.Trim(),
                    IsPreferred = term.IsPreferred
                });
            }

            return copiedTerms;
        }

        private async void AddTerm_Click(object sender, RoutedEventArgs e)
        {
            IThesaurusTerm term = new ThesaurusTerm
            {
                Term = string.Empty,
                NormalizedTerm = string.Empty,
                IsPreferred = false,
                ReferenceName = null
            };

            _conceptDto.Terms.Add(term);
            _conceptDto.Terms.Sort(TermsQuickSort);
            _originalTerms = CopyTerms(_conceptDto.Terms);
            TermsItemsControl.Items.Refresh();
            await FocusTermTextBoxAsync(term);
        }

        private void DeleteTerm_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
            {
                return;
            }

            if (button.DataContext is not IThesaurusTerm term)
            {
                return;
            }

            if (term.IsPreferred)
            {
                return;
            }

            _conceptDto.Terms.Remove(term);
            _conceptDto.NotifyTermChanged();
            _conceptDto.Terms.Sort(TermsQuickSort);
            TermsItemsControl.Items.Refresh();
        }

        private void SetPreferredTerm_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
            {
                return;
            }

            if (button.DataContext is not IThesaurusTerm selectedTerm)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(selectedTerm.Term))
            {
                return;
            }

            foreach (IThesaurusTerm term in _conceptDto.Terms)
            {
                term.IsPreferred = ReferenceEquals(term, selectedTerm);
            }

            _conceptDto.NotifyTermChanged();
            _conceptDto.Terms.Sort(TermsQuickSort);
            TermsItemsControl.Items.Refresh();
        }


        private void TermEditor_Commit(object sender, RoutedEventArgs e)
        {
            if (e is KeyEventArgs keyEventArgs)
            {
                if (keyEventArgs.Key != Key.Enter)
                {
                    return;
                }

                keyEventArgs.Handled = true;
            }

            if (sender is not FrameworkElement termEditor)
            {
                return;
            }

            DependencyProperty? textProperty = termEditor switch
            {
                TextBox => TextBox.TextProperty,
                ComboBox => ComboBox.TextProperty,
                _ => null
            };

            if (textProperty is null)
            {
                return;
            }

            termEditor.GetBindingExpression(textProperty)?.UpdateSource();

            if (termEditor.DataContext is not IThesaurusTerm term)
            {
                return;
            }

            CommitTerm(term);
        }

        private void TermReferenceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox termReferenceComboBox)
            {
                return;
            }

            if (e.AddedItems.Count == 0)
            {
                return;
            }

            if (e.AddedItems[0] is not string referenceName)
            {
                return;
            }

            if (termReferenceComboBox.DataContext is not IThesaurusTerm term)
            {
                return;
            }

            term.ReferenceName = referenceName;

            CommitTerm(term);
        }


        private async Task FocusTermTextBoxAsync(IThesaurusTerm term)
        {
            await Dispatcher.InvokeAsync(TermsItemsControl.UpdateLayout, DispatcherPriority.Loaded);

            TextBox? textBox = FindNewTermTextBox(TermsItemsControl, term);

            textBox?.Focus();
        }

        private static TextBox? FindNewTermTextBox(DependencyObject parent, IThesaurusTerm term)
        {
            for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, index);

                if (child is TextBox textBox &&
                    ReferenceEquals(textBox.DataContext, term) &&
                    textBox.Tag is string tag &&
                    tag == "Term")
                {
                    return textBox;
                }

                TextBox? nestedTextBox = FindNewTermTextBox(child, term);

                if (nestedTextBox is not null)
                {
                    return nestedTextBox;
                }
            }

            return null;
        }

        private void CommitTerm(IThesaurusTerm term)
        {
            term.Term = term.Term?.Trim() ?? string.Empty;
            term.NormalizedTerm = term.Term;

            term.ReferenceName = string.IsNullOrWhiteSpace(term.ReferenceName)
                ? null
                : term.ReferenceName.Trim();

            if (string.IsNullOrWhiteSpace(term.Term) || string.IsNullOrWhiteSpace(term.ReferenceName))
            {
                return;
            }

            int termIndex = _conceptDto.Terms.IndexOf(term);

            IThesaurusTerm originalTerm = _originalTerms[termIndex];

            string originalTermValue = originalTerm.Term?.Trim() ?? string.Empty;

            string? originalReferenceName = string.IsNullOrWhiteSpace(originalTerm.ReferenceName)
                ? null
                : originalTerm.ReferenceName.Trim();

            if (string.Equals(originalTermValue, term.Term, StringComparison.Ordinal) &&
                string.Equals(originalReferenceName, term.ReferenceName, StringComparison.Ordinal))
            {
                return;
            }

            if (!ReferenceNames.Any(existingReferenceName =>
                string.Equals(existingReferenceName, term.ReferenceName, StringComparison.OrdinalIgnoreCase)))
            {
                int insertIndex = 0;

                while (insertIndex < ReferenceNames.Count &&
                    StringComparer.CurrentCultureIgnoreCase.Compare(ReferenceNames[insertIndex], term.ReferenceName) < 0)
                {
                    insertIndex++;
                }

                ReferenceNames.Insert(insertIndex, term.ReferenceName);
            }

            _conceptDto.NotifyTermChanged();
            _conceptDto.Terms.Sort(TermsQuickSort);
            TermsItemsControl.Items.Refresh();
        }

                #endregion

        #region References

        private async void LoadReferencesAsync(object sender, RoutedEventArgs e)
        {
            Loaded -= LoadReferencesAsync;

            List<string> references = await _thesaurusService.GetReferences();
            List<string> referenceNames = [];

            foreach (string reference in references)
            {
                if (string.IsNullOrWhiteSpace(reference))
                {
                    continue;
                }

                string referenceName = reference.Trim();

                if (referenceNames.Any(existingReferenceName =>
                    string.Equals(existingReferenceName, referenceName, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                referenceNames.Add(referenceName);
            }

            referenceNames.Sort(StringComparer.CurrentCultureIgnoreCase);

            foreach (string referenceName in referenceNames)
            {
                ReferenceNames.Add(referenceName);
            }
        }

        #endregion
    }

}
