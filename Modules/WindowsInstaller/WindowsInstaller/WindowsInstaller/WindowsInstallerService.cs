using System.Runtime.InteropServices;
using System.IO;

namespace WindowsInstaller
{
    public class WindowsInstallerService
    {
        private static readonly string[] PropertyNames =
        [
            "Manufacturer",
            "ProductName",
            "ProductVersion",
            "ProductCode",
            "UpgradeCode",
            "SecureCustomProperties"
        ];

        public List<KeyValuePair<string, string>>? GetInfos(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename) || !File.Exists(filename))
                return null;

            dynamic? installer = null;
            dynamic? database = null;
            dynamic? view = null;
            dynamic? record = null;

            try
            {
                Type? installerType = Type.GetTypeFromProgID("WindowsInstaller.Installer");

                if (installerType == null)
                    return null;

                installer = Activator.CreateInstance(installerType);

                if (installer == null)
                    return null;

                database = installer.OpenDatabase(filename, 0);

                view = database.OpenView(
                    "SELECT `Property`, `Value` FROM `Property`");

                view.Execute();

                List<KeyValuePair<string, string>> infos = [];

                while ((record = view.Fetch()) != null)
                {
                    string property = record.StringData[1];
                    string value = record.StringData[2];

                    if (PropertyNames.Contains(property, StringComparer.OrdinalIgnoreCase))
                        infos.Add(new(property, value));

                    ReleaseComObject(record);
                    record = null;
                }

                return infos;
            }
            finally
            {
                ReleaseComObject(record);
                ReleaseComObject(view);
                ReleaseComObject(database);
                ReleaseComObject(installer);
            }
        }

        public List<string>? GetTableList(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename) || !File.Exists(filename))
                return null;

            dynamic? installer = null;
            dynamic? database = null;
            dynamic? view = null;
            dynamic? record = null;

            try
            {
                Type? installerType = Type.GetTypeFromProgID("WindowsInstaller.Installer");
                if (installerType == null)
                    return null;

                installer = Activator.CreateInstance(installerType);
                if (installer == null)
                    return null;

                database = installer.OpenDatabase(filename, 0);
                view = database.OpenView("SELECT * FROM `_Tables`");
                view.Execute();

                List<string> tables = [];

                while ((record = view.Fetch()) != null)
                {
                    tables.Add((string)record.StringData[1]);

                    ReleaseComObject(record);
                    record = null;
                }

                return tables.OrderBy(x => x).ToList();
            }
            finally
            {
                ReleaseComObject(record);
                ReleaseComObject(view);
                ReleaseComObject(database);
                ReleaseComObject(installer);
            }
        }

        public (List<string> Columns, List<List<string>> Rows)? GetTableData( string filename, string tableName)
        {
            if (string.IsNullOrWhiteSpace(filename) ||
                !File.Exists(filename) ||
                string.IsNullOrWhiteSpace(tableName))
                return null;

            dynamic? installer = null;
            dynamic? database = null;
            dynamic? view = null;
            dynamic? record = null;

            try
            {
                Type? installerType = Type.GetTypeFromProgID("WindowsInstaller.Installer");
                if (installerType == null)
                    return null;

                installer = Activator.CreateInstance(installerType);
                if (installer == null)
                    return null;

                database = installer.OpenDatabase(filename, 0);

                List<string> columns = GetColumns(database, tableName);
                if (columns.Count == 0)
                    return null;

                view = database.OpenView($"SELECT * FROM `{tableName}`");
                view.Execute();

                List<List<string>> rows = [];

                while ((record = view.Fetch()) != null)
                {
                    List<string> row = [];

                    for (int column = 1; column <= columns.Count; column++)
                    {
                        try
                        {
                            row.Add((string?)record.StringData[column] ?? string.Empty);
                        }
                        catch
                        {
                            row.Add("[binary]");
                        }
                    }

                    rows.Add(row);

                    ReleaseComObject(record);
                    record = null;
                }

                return (columns, rows);
            }
            finally
            {
                ReleaseComObject(record);
                ReleaseComObject(view);
                ReleaseComObject(database);
                ReleaseComObject(installer);
            }
        }

        private static List<string> GetColumns(dynamic database, string tableName)
        {
            dynamic? view = null;
            dynamic? record = null;

            try
            {
                view = database.OpenView("SELECT * FROM `_Columns`");
                view.Execute();

                List<(int Number, string Name)> columns = [];

                while ((record = view.Fetch()) != null)
                {
                    if ((string)record.StringData[1] == tableName)
                    {
                        columns.Add((
                            int.Parse((string)record.StringData[2]),
                            (string)record.StringData[3]));
                    }

                    ReleaseComObject(record);
                    record = null;
                }

                return columns
                    .OrderBy(x => x.Number)
                    .Select(x => x.Name)
                    .ToList();
            }
            finally
            {
                ReleaseComObject(record);
                ReleaseComObject(view);
            }
        }

        private static void ReleaseComObject(object? value)
        {
            if (value != null && Marshal.IsComObject(value))
                Marshal.FinalReleaseComObject(value);
        }

        public int? GetWordCount(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename) || !File.Exists(filename))
                return null;

            dynamic? installer = null;
            dynamic? summaryInfo = null;

            try
            {
                Type? installerType = Type.GetTypeFromProgID("WindowsInstaller.Installer");

                if (installerType == null)
                    return null;

                installer = Activator.CreateInstance(installerType);

                if (installer == null)
                    return null;

                summaryInfo = installer.SummaryInformation(filename, 0);

                object? value = summaryInfo.Property[15];

                return value == null ? 0 : Convert.ToInt32(value);
            }
            finally
            {
                ReleaseComObject(summaryInfo);
                ReleaseComObject(installer);
            }
        }

        private static readonly (int Id, string Name)[] SummaryProperties =
            [
                (1, "Codepage"),
                (2, "Title"),
                (3, "Subject"),
                (4, "Author"),
                (5, "Keywords"),
                (6, "Comments"),
                (7, "Template"),
                (8, "Last Saved By"),
                (9, "Revision Number"),
                (10, "Edit Time"),
                (11, "Last Printed"),
                (12, "Create Time/Date"),
                (13, "Last Save Time/Date"),
                (14, "Page Count"),
                (15, "Word Count"),
                (16, "Character Count"),
                (17, "Thumbnail"),
                (18, "Creating Application"),
                (19, "Security")
            ];

        public List<KeyValuePair<string, string>>? GetSummaryInfos(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename) || !File.Exists(filename))
                return null;

            dynamic? installer = null;
            dynamic? summaryInfo = null;

            try
            {
                Type? installerType =
                    Type.GetTypeFromProgID("WindowsInstaller.Installer");

                if (installerType == null)
                    return null;

                installer = Activator.CreateInstance(installerType);

                if (installer == null)
                    return null;

                summaryInfo = installer.SummaryInformation(filename, 0);

                List<KeyValuePair<string, string>> infos = [];

                foreach ((int id, string name) in SummaryProperties)
                {
                    string value;

                    try
                    {
                        object? propertyValue = summaryInfo.Property[id];

                        value = propertyValue?.ToString() ?? string.Empty;
                    }
                    catch
                    {
                        value = id == 17
                            ? "[binary]"
                            : string.Empty;
                    }

                    infos.Add(new KeyValuePair<string, string>(name, value));
                }

                return infos;
            }
            finally
            {
                ReleaseComObject(summaryInfo);
                ReleaseComObject(installer);
            }
        }
    }
}