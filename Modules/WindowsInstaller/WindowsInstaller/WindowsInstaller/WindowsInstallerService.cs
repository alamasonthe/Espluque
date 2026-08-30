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

        private static void ReleaseComObject(object? value)
        {
            if (value != null && Marshal.IsComObject(value))
                Marshal.FinalReleaseComObject(value);
        }
    }
}