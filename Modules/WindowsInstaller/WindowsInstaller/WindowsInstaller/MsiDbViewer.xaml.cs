using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace WindowsInstaller
{
    public partial class MsiDbViewer : UserControl
    {
        private readonly WindowsInstallerService _windowsInstallerService = new();
        private readonly string _filename;

        public MsiDbViewer(string filename)
        {
            InitializeComponent();

            _filename = filename;

            LoadTables();
        }

        private void LoadTables()
        {
            List<string>? tables = _windowsInstallerService.GetTableList(_filename);

            if (tables == null)
                return;

            TableList.ItemsSource = tables;

            string? propertyTable = tables.FirstOrDefault(
                table => string.Equals(table, "Property", StringComparison.OrdinalIgnoreCase));

            if (propertyTable != null)
            {
                TableList.SelectedItem = propertyTable;
            }
        }

        private void TableList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TableList.SelectedItem is not string tableName)
                return;

            var tableData = _windowsInstallerService.GetTableData(
                _filename,
                tableName);

            if (tableData == null)
                return;

            List<string> columns = tableData.Value.Columns;
            List<List<string>> rows = tableData.Value.Rows;

            ShowTable( tableData.Value.Columns, tableData.Value.Rows);
        }

        private void ShowTable( List<string> columns, List<List<string>> rows)
        {
            FlowDocument document = new()
            {
                PagePadding = new Thickness(0)
            };

            document.SetResourceReference(
                TextElement.ForegroundProperty,
                "App.Text");

            Table table = new()
            {
                CellSpacing = 0
            };

            foreach (string column in columns)
            {
                table.Columns.Add(new TableColumn());
            }

            TableRowGroup rowGroup = new();

            rowGroup.Rows.Add(BuildHeaderRow(columns));

            foreach (List<string> values in rows)
            {
                TableRow row = new();

                foreach (string value in values)
                {
                    row.Cells.Add(BuildCell(value));
                }

                rowGroup.Rows.Add(row);
            }

            table.RowGroups.Add(rowGroup);
            document.Blocks.Add(table);

            TableRichTextBox.Document = document;
        }

        private TableRow BuildHeaderRow(List<string> columns)
        {
            TableRow row = new();

            foreach (string column in columns)
            {
                row.Cells.Add(BuildCell(column));
            }

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

            cell.SetResourceReference(
                TextElement.ForegroundProperty,
                "App.Text");

            cell.SetResourceReference(
                TableCell.BorderBrushProperty,
                "App.Border");

            return cell;
        }
    }
}