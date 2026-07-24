using Espluque.Application.ModuleManager.Entities;
using System.Text;

namespace Espluque.Application.ModuleManager.Extensions
{
    public static class CatalogEntryExt
    {
        private const int _catalogEntryIndent = 0;

        public static string ToText(this CatalogEntry catalogEntry)
        {
            StringBuilder sb = new StringBuilder();

            AppendProperty(sb, _catalogEntryIndent, "ModuleName", catalogEntry.ModuleName);
            AppendProperty(sb, _catalogEntryIndent, "ModuleVersion", catalogEntry.ModuleVersion);

            AppendProperty(sb, _catalogEntryIndent, "InterfaceType", catalogEntry.InterfaceType);
            AppendProperty(sb, _catalogEntryIndent, "Label", catalogEntry.Label);
            AppendProperty(sb, _catalogEntryIndent, "ClassName", catalogEntry.ClassName);

            if (catalogEntry.Tags is not null && catalogEntry.Tags.Count > 0)
            {
                AppendProperty(sb, _catalogEntryIndent, "Tags", string.Join(", ", catalogEntry.Tags));
            }

            AppendProperty(sb, _catalogEntryIndent, "AssemblyPath", catalogEntry.AssemblyPath);

            AppendLine(sb, _catalogEntryIndent, "------------");

            return sb.ToString();
        }

        #region Helpers

        private static void AppendLine(StringBuilder sb, int indent, string text)
        {
            sb.Append('\t', indent);
            sb.AppendLine(text);
        }

        private static void AppendProperty(StringBuilder sb, int indent, string label, object? value)
        {
            string text = value?.ToString() ?? string.Empty;

            AppendLine(sb, indent, $"{label,-24}: {text}");
        }

        #endregion
    }
}