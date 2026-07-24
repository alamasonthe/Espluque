using Espluque.Contracts.Ports;
using Espluque.Contracts.ModuleInterfaces;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Util;

namespace RegViewer
{
    public partial class RegEdit : UserControl
    {
        private readonly string? _filePath;
        private TreeNode<List<KeyValuePair<string, string>>>? _rootNode;
        private List<string> _leafColumns = [];

        private RegService _regService;
        private readonly ILogger _logger;

        public RegEdit()
        {
            InitializeComponent();
        }

        public RegEdit(string filePath, ILogger logger)
        {
            _filePath = filePath;
            _logger = logger;
            _regService = new(logger);

            InitializeComponent();

            LoadTree(filePath);
        }

        private async void LoadTree(string filePath)
        {
            Result<TreeNode<List<KeyValuePair<string, string>>>> treeResult = await _regService.GetRegistryKeyValueTree(filePath);
            if (!treeResult.IsSuccess)
            {
                LeafDataGrid.Columns.Clear();
                LeafDataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Error",
                    Binding = new Binding("[Error]")
                });

                LeafDataGrid.ItemsSource = new[]
                {
                    new Dictionary<string, string>
                    {
                        ["Error"] = treeResult.Error?.Message ?? "Failed to load registry tree."
                    }
                };

                return;
            }

            _rootNode = treeResult.Value;

            ContainerTreeView.ItemsSource = new[] { _rootNode };

            ContainerTreeView.ApplyTemplate();
            ContainerTreeView.UpdateLayout();

            if (ContainerTreeView.ItemContainerGenerator.ContainerFromItem(_rootNode) is TreeViewItem rootTreeViewItem)
            {
                rootTreeViewItem.IsExpanded = true;
            }

            BuildLeafGridColumns(EnumerateLeafs(_rootNode));

            ShowLeafs(_rootNode);
        }

        private void ContainerTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeNode<List<KeyValuePair<string, string>>> selectedNode)
            {
                ShowLeafs(selectedNode);
            }
        }

        private void ShowLeafs(TreeNode<List<KeyValuePair<string, string>>> node)
        {
            List<Dictionary<string, string>> rows = [];

            foreach (TreeNode<List<KeyValuePair<string, string>>> leaf in node.Children.Where(child => child.IsLeaf))
            {
                Dictionary<string, string> row = new(StringComparer.Ordinal);

                string? internalPath = leaf.Data?.FirstOrDefault(property => property.Key == "Path").Value;
                row["Path"] = internalPath ?? string.Empty;

                if (_leafColumns.Contains("Name"))
                {
                    row["Name"] = string.IsNullOrWhiteSpace(internalPath)
                        ? string.Empty
                        : internalPath.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty;
                }

                foreach (string column in _leafColumns)
                {
                    if (column == "Name")
                    {
                        continue;
                    }

                    string? value = leaf.Data?.FirstOrDefault(property => property.Key == column).Value;
                    row[column] = value ?? string.Empty;
                }

                rows.Add(row);
            }

            LeafDataGrid.ItemsSource = rows;
        }

        private void BuildLeafGridColumns(IEnumerable<TreeNode<List<KeyValuePair<string, string>>>> leafs)
        {
            _leafColumns = BuildLeafColumns(leafs);

            LeafDataGrid.Columns.Clear();

            foreach (string column in _leafColumns)
            {
                LeafDataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = column,
                    Binding = new Binding($"[{column}]")
                });
            }
        }

        private static IEnumerable<TreeNode<List<KeyValuePair<string, string>>>> EnumerateLeafs(TreeNode<List<KeyValuePair<string, string>>> node)
        {
            if (node.IsLeaf)
            {
                yield return node;
                yield break;
            }

            foreach (TreeNode<List<KeyValuePair<string, string>>> child in node.Children)
            {
                foreach (TreeNode<List<KeyValuePair<string, string>>> leaf in EnumerateLeafs(child))
                {
                    yield return leaf;
                }
            }
        }

        private static List<string> BuildLeafColumns(IEnumerable<TreeNode<List<KeyValuePair<string, string>>>> leafs)
        {
            List<string> columns = [];
            HashSet<string> knownColumnKeys = new(StringComparer.Ordinal);

            foreach (TreeNode<List<KeyValuePair<string, string>>> leaf in leafs)
            {
                if (leaf.Data is null)
                {
                    continue;
                }

                foreach (KeyValuePair<string, string> property in leaf.Data)
                {
                    if (string.IsNullOrWhiteSpace(property.Key))
                    {
                        continue;
                    }

                    string columnName = property.Key == "Path" ? "Name" : property.Key;

                    if (knownColumnKeys.Add(columnName))
                    {
                        columns.Add(columnName);
                    }
                }
            }

            return columns;
        }
    }
}