using Espluque.Contracts.Interfaces;
using Espluque.Contracts.ModuleInterfaces;
using Espluquer.Adapters;
using Espluquer.Entities;
using System.Windows;
using System.Windows.Controls;
using Util;

namespace Espluquer.UserControls.Components
{
    public partial class ModuleContributionsUC : UserControl
    {
        private readonly IEntityFactory _entityFactory;

        private readonly IThesaurusService _thesaurusService;
        private TreeNode<IThesaurusConcept>? _tree;

        private readonly List<ICatalogEntry> _catalog;

        private ContributionGraphUC? _contributionGraphUC;
        private string _selectedContributionType = "Viewer";

        private bool _isReloading;

        public ModuleContributionsUC(IThesaurusService thesaurusService, IEntityFactory entityFactory, List<ICatalogEntry> catalog)
        {
            _thesaurusService = thesaurusService;
            _entityFactory = entityFactory;
            _catalog = catalog;

            InitializeComponent();

            IsVisibleChanged += UC_IsVisibleChanged;
        }

        private async void UC_IsVisibleChanged( object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible || _isReloading)
            {
                return;
            }

            _isReloading = true;

            try
            {
                await ReloadAsync();
            }
            finally
            {
                _isReloading = false;
            }
        }

        private async Task ReloadAsync()
        {
            await LoadNodesAsync();

            if (_contributionGraphUC is not null)
            {
                _contributionGraphUC.ThesaurusConceptSelected -= ThesaurusConceptSelected;
            }

            ContributionHost.Content = null;

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
                radioButton.Content is not string selectedContribution)
            {
                return;
            }

            _selectedContributionType = selectedContribution;
            if (_contributionGraphUC is not null)
            {
                _contributionGraphUC.SetContributionTypeAsync(_selectedContributionType);
            }

        }

        private void ThesaurusConceptSelected(int id, string label)
        {
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
        }
    }
}