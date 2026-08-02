using Espluque.Contracts.Interfaces;
using Espluque.Contracts.ModuleInterfaces;
using Espluquer.Adapters;
using Espluquer.UserControls.Thesaurus;
using Espluquer.Entities;
using System.Windows;
using System.Windows.Controls;
using Util;

namespace Espluquer.UserControls.Thesaurus

{
    public partial class ContributionMapUC : RefreshableUserControl
    {
        private readonly IEntityFactory _entityFactory;

        private readonly IThesaurusService _thesaurusService;
        private TreeNode<IThesaurusConcept>? _tree;

        private readonly List<ICatalogEntry> _catalog;

        private ContributionGraphUC? _contributionGraphUC;
        private string _selectedContributionType = "Viewer";

        public ContributionMapUC(IThesaurusService thesaurusService, IEntityFactory entityFactory, List<ICatalogEntry> catalog)
        {
            _thesaurusService = thesaurusService;
            _entityFactory = entityFactory;
            _catalog = catalog;

            InitializeComponent();

        }

        protected override async Task RefreshAsync()
        {
            await ReloadAsync();
        }

        private async Task ReloadAsync()
        {
            await LoadNodesAsync();

            if (_contributionGraphUC is not null)
            {
                _contributionGraphUC.ThesaurusConceptSelected -= ThesaurusConceptSelected;
            }

            SetContributionDetailsVisible(false);

            _contributionGraphUC = new ContributionGraphUC(
                _tree,
                _catalog,
                _selectedContributionType);

            _contributionGraphUC.ThesaurusConceptSelected += ThesaurusConceptSelected;

            GraphHost.Content = _contributionGraphUC;
        }

        private async Task LoadNodesAsync()
        {
            _tree = await _thesaurusService.GetConceptsTree();

        }

        private void ContributionType_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is not RadioButton radioButton ||
                radioButton.Tag is not string selectedContribution)
            {
                return;
            }

            _selectedContributionType = selectedContribution;

            if (_contributionGraphUC is not null)
            {
                _contributionGraphUC.SetContributionTypeAsync(_selectedContributionType);
            }
        }

        private void ThesaurusConceptSelected(int? id, string? label)
        {
            if (id is null)
            {
                SetContributionDetailsVisible(false);
                return;
            }

            if (_tree is null)
            {
                return;
            }

            IThesaurusConcept? FindConcept(TreeNode<IThesaurusConcept> currentNode)
            {
                if (currentNode.Data?.Id == id)
                {
                    return currentNode.Data;
                }

                foreach (TreeNode<IThesaurusConcept> childNode in currentNode.Children)
                {
                    IThesaurusConcept? concept = FindConcept(childNode);

                    if (concept is not null)
                    {
                        return concept;
                    }
                }

                return null;
            }

            IThesaurusConcept? selectedConcept = FindConcept(_tree);

            if (selectedConcept is null)
            {
                return;
            }

            ConceptDto conceptDto = ConceptAdapter.FromDomain(selectedConcept, _entityFactory);

            var detailsUC = new ContributionDetailsUC(conceptDto, _catalog);

            ContributionHost.Content = detailsUC;
            SetContributionDetailsVisible(true);
        }

        private void SetContributionDetailsVisible(bool isVisible)
        {
            ContributionSplitterColumn.Width = isVisible
                ? new GridLength(3)
                : new GridLength(0);

            ContributionColumn.Width = isVisible
                ? new GridLength(1, GridUnitType.Star)
                : new GridLength(0);

            ContributionSplitter.Visibility = isVisible
                ? Visibility.Visible
                : Visibility.Collapsed;

            ContributionHost.Visibility = isVisible
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (!isVisible)
            {
                ContributionHost.Content = null;
            }
        }
    }
}