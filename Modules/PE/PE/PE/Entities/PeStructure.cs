using Espluque.Contracts.CrossCutting;

namespace PE.Entities
{
    internal abstract class PeStructure
    {
        protected readonly string _filePath;
        protected long? _structureStartOffset;
        internal long? StructureStartOffset => _structureStartOffset;
        protected readonly ILogger _logger;

        protected PeFile? Root { get; set; }

        protected PeStructure(string filePath, ILogger logger)
        {
            _filePath = filePath;
            _logger = logger;
        }

        protected PeStructure(PeFile root, string filePath, ILogger logger)
        {
            Root = root;
            _filePath = filePath;
            _logger = logger;
        }

        internal object? GetValue(string path)
        {
            int separatorIndex = path.IndexOf('.');
            string segment = separatorIndex >= 0 ? path[..separatorIndex] : path;
            string remainingPath = separatorIndex >= 0 ? path[(separatorIndex + 1)..] : string.Empty;

            int bracketIndex = segment.IndexOf('[');
            string propertyName = bracketIndex >= 0 ? segment[..bracketIndex] : segment;

            var property = GetType().GetProperty(propertyName);
            if (property is null)
                return null;

            object? propertyValue = property.GetValue(this);
            if (propertyValue is null)
                return null;

            if (bracketIndex >= 0)
            {
                int closingBracketIndex = segment.IndexOf(']', bracketIndex);
                if (closingBracketIndex < 0)
                    return null;
                string indexText = segment[(bracketIndex + 1)..closingBracketIndex];
                if (!int.TryParse(indexText, out int index))
                    return null;
                if (propertyValue is not System.Collections.IList list || index < 0 || index >= list.Count)
                    return null;
                propertyValue = list[index];
            }

            if (separatorIndex < 0)
            {
                if (propertyValue is PeField field)
                    return field.Value;
                return propertyValue;
            }

            if (propertyValue is PeStructure structure)
                return structure.GetValue(remainingPath);

            return null;
        }
    }
}