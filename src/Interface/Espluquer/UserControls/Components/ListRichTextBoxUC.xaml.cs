using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Espluquer.Usercontrols.Components
{
    public partial class ListRichTextBoxUC : UserControl
    {
        private const double KeyColumnWidth = 220;
        private const double MinimumValueColumnWidth = 120;

        private readonly List<KeyValuePair<string, string?>> _items;

        private TableColumn? _keyColumn;
        private TableColumn? _valueColumn;

        public ListRichTextBoxUC(List<KeyValuePair<string, string?>> items)
        {
            _items = items;

            InitializeComponent();

            ContentRichTextBox.Document = BuildDocument(_items);
        }

        private FlowDocument BuildDocument(List<KeyValuePair<string, string?>> items)
        {
            if (items is null || items.Count == 0) return null;

            FlowDocument document = new()
            {
                PagePadding = new Thickness(0)
            };

            document.SetResourceReference(TextElement.ForegroundProperty, "App.Text");

            Table table = new()
            {
                CellSpacing = 0
            };

            _keyColumn = new TableColumn { Width = new GridLength(KeyColumnWidth) };
            _valueColumn = new TableColumn { Width = new GridLength(MinimumValueColumnWidth) };

            table.Columns.Add(_keyColumn);
            table.Columns.Add(_valueColumn);

            TableRowGroup contentGroup = new();

            foreach (KeyValuePair<string, string?> item in items)
            {
                TableRow row = new();

                row.Cells.Add(BuildValueCell(item.Key));
                row.Cells.Add(BuildValueCell(item.Value ?? string.Empty));

                contentGroup.Rows.Add(row);
            }

            table.RowGroups.Add(contentGroup);
            document.Blocks.Add(table);

            return document;
        }

        private TableCell BuildValueCell(string text)
        {
            return BuildCell(text);
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

        private void ContentRichTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateDocumentLayout();
        }

        private void ContentRichTextBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateDocumentLayout();
        }

        private void UpdateDocumentLayout()
        {
            if (_keyColumn is null || _valueColumn is null)
            {
                return;
            }

            double documentWidth = ContentRichTextBox.ActualWidth - SystemParameters.VerticalScrollBarWidth;

            if (documentWidth <= 0)
            {
                return;
            }

            double valueColumnWidth = documentWidth - KeyColumnWidth;

            if (valueColumnWidth < MinimumValueColumnWidth)
            {
                valueColumnWidth = MinimumValueColumnWidth;
            }

            ContentRichTextBox.Document.PageWidth = documentWidth;
            ContentRichTextBox.Document.MinPageWidth = documentWidth;
            ContentRichTextBox.Document.MaxPageWidth = documentWidth;
            ContentRichTextBox.Document.ColumnWidth = documentWidth;

            _keyColumn.Width = new GridLength(KeyColumnWidth);
            _valueColumn.Width = new GridLength(valueColumnWidth);
        }
    }
}