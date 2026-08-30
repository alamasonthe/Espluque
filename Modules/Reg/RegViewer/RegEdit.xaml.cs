using Espluque.Contracts.CrossCutting;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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
            Result<TreeNode<List<KeyValuePair<string, string>>>> treeResult =
                await _regService.GetRegistryKeyValueTree(filePath);

            if (!treeResult.IsSuccess)
            {
                FlowDocument document = new()
                {
                    PagePadding = new Thickness(0)
                };

                document.SetResourceReference(TextElement.ForegroundProperty, "App.Text");

                Paragraph paragraph = new(
                    new Run(treeResult.Error?.Message ?? "Failed to load registry tree."))
                {
                    Margin = new Thickness(8, 5, 8, 5)
                };

                document.Blocks.Add(paragraph);

                LeafRichTextBox.Document = document;

                return;
            }

            _rootNode = treeResult.Value;

            ContainerTreeView.ItemsSource = _rootNode.BranchChildren;

            ShowLeafs(_rootNode);
        }

        private void ContainerTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeNode<List<KeyValuePair<string, string>>> selectedNode)
            {
                ShowLeafs(selectedNode);
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

        private void ShowLeafs(TreeNode<List<KeyValuePair<string, string>>> node)
        {
            FlowDocument document = new()
            {
                PagePadding = new Thickness(0)
            };

            document.SetResourceReference(TextElement.ForegroundProperty, "App.Text");

            Table table = new()
            {
                CellSpacing = 0
            };

            table.Columns.Add(new TableColumn { Width = new GridLength(220) });
            table.Columns.Add(new TableColumn { Width = new GridLength(130) });
            table.Columns.Add(new TableColumn());

            TableRowGroup rows = new();

            rows.Rows.Add(BuildHeaderRow());

            foreach (TreeNode<List<KeyValuePair<string, string>>> leaf
                     in node.Children.Where(child => child.IsLeaf))
            {
                string path =
                    leaf.Data?.FirstOrDefault(x => x.Key == "Path").Value
                    ?? string.Empty;

                string name = path
                    .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries)
                    .LastOrDefault()
                    ?? string.Empty;

                string type =
                    leaf.Data?.FirstOrDefault(x => x.Key == "Type").Value
                    ?? string.Empty;

                string data =
                    leaf.Data?.FirstOrDefault(x => x.Key == "Data").Value
                    ?? string.Empty;

                TableRow row = new();

                row.Cells.Add(BuildCell(name));
                row.Cells.Add(BuildCell(type));
                row.Cells.Add(BuildCell(data));

                rows.Rows.Add(row);
            }

            table.RowGroups.Add(rows);
            document.Blocks.Add(table);

            LeafRichTextBox.Document = document;
        }

        #region helpers

        private TableRow BuildHeaderRow()
        {
            TableRow row = new();

            row.Cells.Add(BuildCell("Name"));
            row.Cells.Add(BuildCell("Type"));
            row.Cells.Add(BuildCell("Data"));

            return row;
        }

        private TableCell BuildCell(string text)
        {
            Paragraph paragraph = new(new Run(text))
            {
                Margin = new Thickness(0)
            };

            TableCell cell = new(paragraph)
            {
                Padding = new Thickness(8, 5, 8, 5),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };

            cell.SetResourceReference(TextElement.ForegroundProperty, "App.Text");
            cell.SetResourceReference(TableCell.BorderBrushProperty, "App.Border");

            return cell;
        }

        #endregion
    }
}