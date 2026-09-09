using Espluque.Contracts.CrossCutting;
using PE.Enums;
using PE.Repositories;
using PE.Services;

namespace PE.Entities
{
    internal class PeRsrcDirectoryTable : PeStructure
    {
        private PeRsrcDirectoryLevel _level { get; set; }

        internal bool _isLoaded = false;
        internal PeField[] _fields = [];
        internal List<PeRsrcDirectoryEntry> _entries = [];

        public PeField? Characteristics { get { if (!EnsureLoaded()) return null; return _fields.First(item => item.Name == nameof(Characteristics)); } }
        public PeField? TimeDateStamp { get { if (!EnsureLoaded()) return null; return _fields.First(item => item.Name == nameof(TimeDateStamp)); } }
        public PeField? MajorVersion { get { if (!EnsureLoaded()) return null; return _fields.First(item => item.Name == nameof(MajorVersion)); } }
        public PeField? MinorVersion { get { if (!EnsureLoaded()) return null; return _fields.First(item => item.Name == nameof(MinorVersion)); } }
        public PeField? NumberOfNamedEntries { get { if (!EnsureLoaded()) return null; return _fields.First(item => item.Name == nameof(NumberOfNamedEntries)); } }
        public PeField? NumberOfIdEntries { get { if (!EnsureLoaded()) return null; return _fields.First(item => item.Name == nameof(NumberOfIdEntries)); } }
        public IReadOnlyList<PeRsrcDirectoryEntry>? Entries { get { if (!EnsureLoaded()) return null; return _entries; } }

        public PeRsrcDirectoryTable(PeFile root, string filePath, long structureStartOffset, PeRsrcDirectoryLevel level, ILogger logger)
            : base(root, filePath, logger)
        {
            _structureStartOffset = structureStartOffset;
            _level = level;
        }

        private bool EnsureLoaded()
        {
            if (!_isLoaded)
            {
                if (!LoadStructureDefinition())
                    return false;
                if (!LoadStructureData())
                    return false;
                if (!LoadEntries())
                    return false;
                _isLoaded = true;
            }
            return true;
        }

        private bool LoadStructureDefinition()
        {
            PeRepository repository = new();
            var fieldsResult = repository.GetFields("RsrcDirectoryTable");
            if (!fieldsResult.IsSuccess)
            {
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"Failed to retrieve resource directory table fields: {fieldsResult.Error?.Message}");
                return false;
            }
            _fields = fieldsResult.Value!;
            return true;
        }

        private bool LoadStructureData()
        {
            if (_structureStartOffset is null)
                return false;
            PeReader reader = new();
            for (int i = 0; i < _fields.Length; i++)
            {
                var result = reader.ReadField(_filePath, _structureStartOffset.Value, _fields[i]);
                if (!result.IsSuccess)
                {
                    _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"Failed to read resource directory table field {_fields[i].Name}: {result.Error?.Message}");
                    return false;
                }
                _fields[i] = result.Value!;
            }
            return true;
        }

        private bool LoadEntries()
        {
            if (_structureStartOffset is null)
                return false;
            PeField? namedEntriesField = _fields.FirstOrDefault(item => item.Name == nameof(NumberOfNamedEntries));
            PeField? idEntriesField = _fields.FirstOrDefault(item => item.Name == nameof(NumberOfIdEntries));
            if (namedEntriesField?.Value is null || idEntriesField?.Value is null)
                return false;
            int entryCount = Convert.ToInt32(namedEntriesField.Value) + Convert.ToInt32(idEntriesField.Value);
            long entriesStartOffset = _structureStartOffset.Value + 16;
            _entries = [];
            for (int i = 0; i < entryCount; i++)
                _entries.Add(new PeRsrcDirectoryEntry(Root!, _filePath, entriesStartOffset + (i * 8), _level, _logger));
            return true;
        }
    }
}