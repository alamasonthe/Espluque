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

            string propertyName = separatorIndex >= 0 ? path[..separatorIndex] : path;

            var property = GetType().GetProperty(propertyName);
            bool propertyExists = property is not null;

            if (!propertyExists) {
                return null;
            }

            bool isTerminal = separatorIndex < 0;

            if (isTerminal && property!.PropertyType == typeof(PeField))
            {
                PeField? field = property.GetValue(this) as PeField;
                return field?.Value;
            }

            if (separatorIndex >= 0 && typeof(PeStructure).IsAssignableFrom(property!.PropertyType))
            {
                PeStructure? structure = property.GetValue(this) as PeStructure;
                string remainingPath = path[(separatorIndex + 1)..];

                return structure?.GetValue(remainingPath);
            }

            return null;
        }
    }
}