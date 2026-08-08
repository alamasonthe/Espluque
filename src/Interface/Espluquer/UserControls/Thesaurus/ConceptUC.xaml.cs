using Espluque.Contracts.Interfaces;
using Espluquer.Adapters;
using Espluquer.Entities;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Util;
using Espluquer.UserControls.Shell;

namespace Espluquer.UserControls.Thesaurus
{
    public partial class ConceptUC : RefreshableUserControl
    {
        private readonly IThesaurusService _thesaurusService;
        private readonly IEntityFactory _entityFactory;
        private readonly ConceptSearchUC _conceptSearchUC;
        private TreeNode<IThesaurusConcept>? _rootNode;

        private ConceptDto? _selectedConceptDto;
        private Point? _dragStartPoint;

        public ConceptUC(IThesaurusService thesaurusService, IEntityFactory entityFactory, ConceptSearchUC conceptSearchUC)
        {
            _thesaurusService = thesaurusService;
            _entityFactory = entityFactory;
            _conceptSearchUC = conceptSearchUC;

            InitializeComponent();

            ConceptSearchControl.Content = conceptSearchUC;
            conceptSearchUC.ConceptSelected += Search_ConceptSelected;
        }

        protected override async Task RefreshAsync()
        {
            await LoadTreeAsync();
        }

        private async Task LoadTreeAsync(HashSet<int>? expandedConceptIds = null)
        {
            TreeNode<IThesaurusConcept>? tree = await _thesaurusService.GetConceptsTree();

            if (tree is null)
            {
                ConceptTreeView.ItemsSource = null;
                return;
            }

            _rootNode = tree;

            ConceptTreeView.ItemsSource = new[] { _rootNode };

            if (expandedConceptIds is not null)
            {
                await Dispatcher.InvokeAsync(ConceptTreeView.UpdateLayout, DispatcherPriority.Loaded);
                await ExpandBranchesAsync(ConceptTreeView, expandedConceptIds);
            }
        }

        private void RefreshTreeView()
        {
            if (_rootNode is null)
            {
                ConceptTreeView.ItemsSource = null;
                return;
            }

            ConceptTreeView.ItemsSource = null;
            ConceptTreeView.ItemsSource = new[] { _rootNode };
        }

        private void ConceptTreeView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (sender is not TextBox textBox)
            {
                return;
            }

            if (textBox.DataContext is not TreeNode<IThesaurusConcept> selectedNode)
            {
                return;
            }

            ContextMenu contextMenu = new();

            MenuItem createChildMenuItem = new()
            {
                Header = "Create New Context",
                CommandParameter = selectedNode
            };

            createChildMenuItem.Click += CreateNewContext_Click;
            contextMenu.Items.Add(createChildMenuItem);

            if (selectedNode.Data is not null)
            {
                MenuItem deleteMenuItem = new()
                {
                    Header = "Delete",
                    CommandParameter = selectedNode
                };

                deleteMenuItem.Click += DeleteMenuItem_Click;
                contextMenu.Items.Add(deleteMenuItem);
            }

            textBox.ContextMenu = contextMenu;
        }

        private void ConceptNameTextBox_EnterEditMode(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TextBox textBox)
            {
                return;
            }

            if (textBox.DataContext is not TreeNode<IThesaurusConcept> editedNode)
            {
                return;
            }

            if (editedNode.Data is null)
            {
                return;
            }

            textBox.LostFocus -= ConceptNameTextBox_LostFocus;
            textBox.KeyDown -= ConceptNameTextBox_KeyDown;
            textBox.LostFocus += ConceptNameTextBox_LostFocus;
            textBox.KeyDown += ConceptNameTextBox_KeyDown;

            SetConceptNameTextBoxEditStyle(textBox);
            textBox.Focus();
            textBox.SelectAll();

            e.Handled = true;
        }

        private void ConceptTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_selectedConceptDto is not null)
            {
                _selectedConceptDto.PropertyChanged -= ConceptChanged;
            }

            if (e.NewValue is not TreeNode<IThesaurusConcept> selectedNode)
            {
                ConceptDetailsHost.Content = null;
                ConceptGraphHost.Content = null;
                return;
            }

            if (selectedNode.Data is null)
            {
                ConceptDetailsHost.Content = null;
                ConceptGraphHost.Content = null;
                return;
            }

            _selectedConceptDto = ConceptAdapter.FromDomain(selectedNode.Data, _entityFactory);
            _selectedConceptDto.PropertyChanged += ConceptChanged;

            ThesaurusConceptDetailsUC conceptDetails = new(_selectedConceptDto, _thesaurusService);
            conceptDetails.DeleteRequested += ConceptDetails_DeleteRequested;

            ConceptDetailsHost.Content = conceptDetails;
            ConceptGraphHost.Content = new ThesaurusConceptGraph(_selectedConceptDto, _thesaurusService);
        }

        #region Drag&Drop add parent/child link

        private void ConceptSaveDragStartPoint(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(ConceptTreeView);
        }

        private void ConceptDragStart(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                _dragStartPoint = null;
                return;
            }

            if (_dragStartPoint is not Point dragStartPoint)
            {
                return;
            }

            Point currentPosition = e.GetPosition(ConceptTreeView);

            if (Math.Abs(currentPosition.X - dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(currentPosition.Y - dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            if (sender is not TextBox textBox)
            {
                return;
            }

            if (textBox.DataContext is not TreeNode<IThesaurusConcept> draggedNode)
            {
                return;
            }

            if (draggedNode.Data is not IThesaurusConcept draggedConcept)
            {
                return;
            }

            _dragStartPoint = null;

            DataObject dataObject = new();
            dataObject.SetData(typeof(IThesaurusConcept), draggedConcept);

            DragDrop.DoDragDrop(textBox, dataObject, DragDropEffects.Copy);

            e.Handled = true;
        }

        private void ConceptAllowDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(IThesaurusConcept)))
            {
                return;
            }

            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private async void ConceptDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(IThesaurusConcept)))
            {
                return;
            }

            e.Effects = DragDropEffects.Copy;
            e.Handled = true;

            if (sender is not TextBox targetTextBox)
            {
                return;
            }

            if (targetTextBox.DataContext is not TreeNode<IThesaurusConcept> targetNode)
            {
                return;
            }

            if (targetNode.Data?.Id is not int parentConceptId)
            {
                return;
            }

            if (e.Data.GetData(typeof(IThesaurusConcept)) is not IThesaurusConcept childConcept)
            {
                return;
            }

            if (childConcept.Id is not int childConceptId)
            {
                return;
            }

            bool isSaved = await _thesaurusService.SaveParentChildLink(parentConceptId, childConceptId);

            if (!isSaved)
            {
                return;
            }

            HashSet<int> expandedConceptIds = ListExpandedBranchConceptIds(ConceptTreeView).ToHashSet();
            expandedConceptIds.Add(parentConceptId);

            await LoadTreeAsync(expandedConceptIds);
            await SelectTreeViewItem(childConceptId);
        }

        #endregion


        #region Create new concept

        private async void CreateNewContext_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem)
            {
                return;
            }

            if (menuItem.CommandParameter is not TreeNode<IThesaurusConcept> parentNode)
            {
                return;
            }

            await CreateNewConcept(parentNode);
        }

        private async Task CreateNewConcept(TreeNode<IThesaurusConcept> parentNode)
        {
            try
            {
                string newConceptName = CreateUniqueChildConceptName(parentNode);
                IThesaurusTerm preferredTerm = _entityFactory.CreateThesaurusTerm(newConceptName, newConceptName, true, "Espluque");

                List<IThesaurusConcept> parents = parentNode.Data is null
                    ? []
                    : [parentNode.Data];

                IThesaurusConcept newConcept = _entityFactory.CreateThesaurusConcept(
                    null,
                    [preferredTerm],
                    parents,
                    []);

                TreeNode<IThesaurusConcept> newConceptNode = new()
                {
                    Name = newConceptName,
                    FullPath = string.Empty,
                    IsLeaf = false,
                    Data = newConcept
                };

                HashSet<int> expandedConceptIds = ListExpandedBranchConceptIds(ConceptTreeView).ToHashSet();

                parentNode.Children.Add(newConceptNode);

                RefreshTreeView();
                await Dispatcher.InvokeAsync(ConceptTreeView.UpdateLayout, DispatcherPriority.Loaded);

                await ExpandBranchesAsync(ConceptTreeView, expandedConceptIds);

                TreeViewItem? parentTreeViewItem = FindTreeViewItemByConceptId(ConceptTreeView, parentNode.Data?.Id);

                if (parentTreeViewItem is null)
                {
                    return;
                }

                parentTreeViewItem.IsExpanded = true;
                parentTreeViewItem.BringIntoView();

                TreeViewItem? newConceptTreeViewItem = await WaitForTreeViewItemAsync(parentTreeViewItem, newConceptNode);

                if (newConceptTreeViewItem is null)
                {
                    return;
                }

                newConceptTreeViewItem.IsSelected = true;

                TextBox? newConceptTextBox = FindVisualChild<TextBox>(newConceptTreeViewItem);

                newConceptTextBox!.LostFocus += ConceptNameTextBox_LostFocus;
                newConceptTextBox!.KeyDown += ConceptNameTextBox_KeyDown;

                SetConceptNameTextBoxEditStyle(newConceptTextBox);
                newConceptTextBox.Focus();
                newConceptTextBox.SelectAll();
            }
            catch (Exception exception)
            {
                MessageBox.Show($"erreur {exception.HResult}: {exception.Message}");
            }
        }

        private static string CreateUniqueChildConceptName(TreeNode<IThesaurusConcept> parentNode)
        {
            const string baseName = "New concept";

            List<string> existingNames = [];

            foreach (TreeNode<IThesaurusConcept> childNode in parentNode.Children)
            {
                string childName = childNode.Data?.Terms
                    .FirstOrDefault(term => term.IsPreferred)?
                    .Term ?? childNode.Name;

                if (!string.IsNullOrWhiteSpace(childName))
                {
                    existingNames.Add(childName);
                }
            }

            if (!existingNames.Contains(baseName, StringComparer.Ordinal))
            {
                return baseName;
            }

            int index = 2;
            string candidateName = $"{baseName} {index}";

            while (existingNames.Contains(candidateName, StringComparer.Ordinal))
            {
                index++;
                candidateName = $"{baseName} {index}";
            }

            return candidateName;
        }

        #endregion


        #region Save

        private async void ConceptNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox textBox)
            {
                return;
            }

            await SaveNewConceptAsync(textBox);
        }

        private async void ConceptNameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            if (sender is not TextBox textBox)
            {
                return;
            }

            e.Handled = true;

            await SaveNewConceptAsync(textBox);
        }

        private async Task SaveNewConceptAsync(TextBox textBox)
        {
            textBox.LostFocus -= ConceptNameTextBox_LostFocus;
            textBox.KeyDown -= ConceptNameTextBox_KeyDown;

            SetConceptNameTextBoxReadOnlyStyle(textBox);

            if (textBox.DataContext is not TreeNode<IThesaurusConcept> editedNode)
            {
                return;
            }

            if (editedNode.Data is not IThesaurusConcept editedConcept)
            {
                return;
            }

            IThesaurusTerm? preferredTerm = editedConcept.Terms.FirstOrDefault(term => term.IsPreferred);

            if (preferredTerm is null)
            {
                return;
            }

            string textBoxTerm = textBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(textBoxTerm))
            {
                await LoadTreeAsync();
                return;
            }

            preferredTerm.Term = textBoxTerm;
            preferredTerm.NormalizedTerm = textBoxTerm;

            if (_selectedConceptDto is not null)
            {
                _selectedConceptDto.PropertyChanged -= ConceptChanged;
            }

            _selectedConceptDto = ConceptAdapter.FromDomain(editedConcept, _entityFactory);

            await SaveConceptAsync();
        }

        private async void ConceptChanged(object? sender, PropertyChangedEventArgs e)
        {
            await SaveConceptAsync();
        }

        private async Task<int?> SaveConceptAsync()
        {
            int? conceptId = null;

            if (!(_selectedConceptDto is null))
            {
                IThesaurusConcept concept = ConceptAdapter.ToDomain(_selectedConceptDto, _entityFactory);

                conceptId = await _thesaurusService.SaveConcept(concept);

                if (!(conceptId is null))
                {
                    _selectedConceptDto.Id = conceptId;
                }
            }

            HashSet<int> expandedConceptIds = ListExpandedBranchConceptIds(ConceptTreeView).ToHashSet();

            await LoadTreeAsync(expandedConceptIds);
            await SelectTreeViewItem(conceptId);

            return conceptId;
        }

        #endregion


        #region Delete

        private async void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem)
            {
                return;
            }

            if (menuItem.CommandParameter is not TreeNode<IThesaurusConcept> selectedNode)
            {
                return;
            }

            await DeleteConceptAsync(selectedNode);
        }

        private async void ConceptDetails_DeleteRequested(object? sender, EventArgs e)
        {
            if (ConceptTreeView.SelectedItem is not TreeNode<IThesaurusConcept> selectedNode)
            {
                return;
            }

            await DeleteConceptAsync(selectedNode);
        }

        private async Task DeleteConceptAsync(TreeNode<IThesaurusConcept> selectedNode)
        {
            if (selectedNode.Data?.Id is not int conceptId)
            {
                return;
            }

            HashSet<int> expandedConceptIds = ListExpandedBranchConceptIds(ConceptTreeView).ToHashSet();

            TreeNode<IThesaurusConcept>? parentNode = selectedNode.Parent;

            int? parentConceptId = parentNode?.Data?.Id;

            bool isDeleted = await _thesaurusService.DeleteConcept(conceptId);

            if (!isDeleted)
            {
                return;
            }

            await LoadTreeAsync(expandedConceptIds);
            await SelectTreeViewItem(parentConceptId);
        }
        #endregion


        #region Helpers

        private void SetConceptNameTextBoxReadOnlyStyle(TextBox textBox)
        {
            textBox.Style = (Style)FindResource("App.ReadOnlyTextBox");
            textBox.IsReadOnly = true;
            textBox.Focusable = false;
        }

        private void SetConceptNameTextBoxEditStyle(TextBox textBox)
        {
            textBox.Style = (Style)FindResource("App.StandardTextBox");
            textBox.IsReadOnly = false;
            textBox.Focusable = true;
        }

        private async Task SelectTreeViewItem(int? ConceptId)
        {
            await Dispatcher.InvokeAsync(ConceptTreeView.UpdateLayout, DispatcherPriority.Loaded);

            if (ConceptId is not int conceptId)
            {
                return;
            }

            TreeViewItem? selectedTreeViewItem = FindTreeViewItemByConceptId(ConceptTreeView, conceptId);
            if (selectedTreeViewItem is null)
            {
                return;
            }

            selectedTreeViewItem.IsSelected = true;
            selectedTreeViewItem.Focus();
        }

        private static IEnumerable<int> ListExpandedBranchConceptIds(ItemsControl parent)
        {
            List<int> expandedBranchConceptIds = [];

            foreach (object item in parent.Items)
            {
                if (parent.ItemContainerGenerator.ContainerFromItem(item) is not TreeViewItem treeViewItem)
                {
                    continue;
                }

                if (treeViewItem.IsExpanded &&
                    item is TreeNode<IThesaurusConcept> node &&
                    node.Data?.Id is int conceptId)
                {
                    expandedBranchConceptIds.Add(conceptId);
                }

                expandedBranchConceptIds.AddRange(ListExpandedBranchConceptIds(treeViewItem));
            }

            return expandedBranchConceptIds;
        }

        private static TreeViewItem? FindTreeViewItemByConceptId(ItemsControl parent, int? conceptId)
        {
            foreach (object item in parent.Items)
            {
                if (parent.ItemContainerGenerator.ContainerFromItem(item) is not TreeViewItem treeViewItem)
                {
                    continue;
                }

                if (item is TreeNode<IThesaurusConcept> node)
                {
                    bool isMatchingNode = conceptId is null
                        ? node.Data is null
                        : node.Data?.Id == conceptId;

                    if (isMatchingNode)
                    {
                        return treeViewItem;
                    }
                }

                TreeViewItem? childTreeViewItem = FindTreeViewItemByConceptId(treeViewItem, conceptId);

                if (childTreeViewItem is not null)
                {
                    return childTreeViewItem;
                }
            }

            return null;
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild)
                {
                    return typedChild;
                }

                T? nestedChild = FindVisualChild<T>(child);
                if (nestedChild is not null)
                {
                    return nestedChild;
                }
            }

            return null;
        }

        private async Task ExpandBranchesAsync(ItemsControl parent, HashSet<int> expandedConceptIds)
        {
            foreach (object item in parent.Items)
            {
                if (parent.ItemContainerGenerator.ContainerFromItem(item) is not TreeViewItem treeViewItem)
                {
                    continue;
                }

                if (item is TreeNode<IThesaurusConcept> node &&
                    (node.Data is null || node.Data.Id is int conceptId && expandedConceptIds.Contains(conceptId)))
                {
                    treeViewItem.IsExpanded = true;
                    await Dispatcher.InvokeAsync(treeViewItem.UpdateLayout, DispatcherPriority.Loaded);
                }

                await ExpandBranchesAsync(treeViewItem, expandedConceptIds);
            }
        }

        private async Task<TreeViewItem?> WaitForTreeViewItemAsync(ItemsControl parent, object item)
        {
            for (int attempt = 0; attempt < 10; attempt++)
            {
                await Dispatcher.InvokeAsync(parent.UpdateLayout, DispatcherPriority.Loaded);

                TreeViewItem? treeViewItem = parent.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;

                if (treeViewItem is not null)
                {
                    return treeViewItem;
                }

                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ContextIdle);
            }

            return parent.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
        }

        #endregion


        #region Search and select concept

        private async void Search_ConceptSelected(ConceptDto concept)
        {
            await SelectConcept(concept.Id);
            _conceptSearchUC.Clear();
        }

        private async Task SelectConcept(int? conceptId)
        {
            if (_rootNode is null || conceptId is not int id)
            {
                return;
            }

            TreeNode<IThesaurusConcept>? node =
                FindTreeNodeByConceptId(_rootNode, id);

            if (node is null)
            {
                return;
            }

            HashSet<int> parentConceptIds = [];

            TreeNode<IThesaurusConcept>? parent = node.Parent;

            while (parent is not null)
            {
                if (parent.Data?.Id is int parentId)
                {
                    parentConceptIds.Add(parentId);
                }

                parent = parent.Parent;
            }

            await ExpandBranchesAsync(ConceptTreeView, parentConceptIds);
            await SelectTreeViewItem(id);
        }

        private static TreeNode<IThesaurusConcept>? FindTreeNodeByConceptId( TreeNode<IThesaurusConcept> node, int conceptId)
        {
            if (node.Data?.Id == conceptId)
            {
                return node;
            }

            foreach (TreeNode<IThesaurusConcept> child in node.Children)
            {
                TreeNode<IThesaurusConcept>? foundNode =
                    FindTreeNodeByConceptId(child, conceptId);

                if (foundNode is not null)
                {
                    return foundNode;
                }
            }

            return null;
        }

        #endregion

    }
}
