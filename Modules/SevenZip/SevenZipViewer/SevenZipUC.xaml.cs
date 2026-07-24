using Espluque.Contracts.Enums;
using Espluque.Contracts.Interfaces;
using Espluque.Contracts.MessageInterfaces;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Util;

namespace SevenZipViewer
{
    public partial class SevenZipUC : UserControl
    {
        private readonly IMessageCenter _messageCenter;
        private readonly IEntityFactory _entityFactory;

        private TreeNode<List<KeyValuePair<string, string>>>? _rootNode;
        private List<string> _leafColumns = [];

        private readonly string? _filePath;

        public SevenZipUC(string filePath, IMessageCenter messageCenter, IEntityFactory entityFactory)
        {
            _filePath = filePath;
            _messageCenter = messageCenter;
            _entityFactory = entityFactory;

            InitializeComponent();

            LoadTree(filePath);
        }


        #region Load tree

        private void LoadTree(string filePath)
        {
            Result<TreeNode<List<KeyValuePair<string, string>>>> treeResult = SevenZip.Services.SevenZipService.GetTree(filePath);
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
                        ["Error"] = treeResult.Error?.Message ?? "Failed to load container tree."
                    }
                };

                return;
            }

            _rootNode = treeResult.Value;

            ContainerTreeView.ItemsSource = new[] { _rootNode };

            BuildLeafGridColumns(EnumerateLeafs(_rootNode));

            ShowLeafs(_rootNode);

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

        #endregion


        #region Show Leafs

        private void ContainerTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeNode<List<KeyValuePair<string, string>>> selectedNode)
            {
                ShowLeafs(selectedNode);
            }
        }

        #endregion


        #region Message to open & analyze embedded file

        private void LeafDataGrid_MouseDoubleClickOld(object sender, MouseButtonEventArgs e)
        {
            if (LeafDataGrid.SelectedItem is not Dictionary<string, string> row)
            {
                return;
            }

            row.TryGetValue("Path", out string? internalPath);

        }

        private async void LeafDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (LeafDataGrid.SelectedItem is not Dictionary<string, string> row)
            {
                return;
            }

            if (!row.TryGetValue("Path", out string? internalPath) ||
                string.IsNullOrWhiteSpace(internalPath))
            {
                return;
            }

            IMessage message = _entityFactory.CreateMessage(
                MessageTypeEnum.ExtractAndAnalyze,
                "ExtractAndAnalyze",
                [
                    new("FilePath", _filePath),
                    new("InternalPath", internalPath)
                ]
                );

            await _messageCenter.SendAsync(message);

            e.Handled = true;
        }

        #endregion
    }
}
