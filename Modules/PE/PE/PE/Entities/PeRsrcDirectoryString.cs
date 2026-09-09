using Espluque.Contracts.CrossCutting;
using PE.Repositories;
using PE.Services;

namespace PE.Entities
{
    internal class PeRsrcDirectoryString : PeStructure
    {
        internal bool _isLoaded = false;
        internal PeField[] _fields = [];

        #region Properties

        public PeField? Length { get { if (!EnsureLoaded()) return null; return _fields.First(item => item.Name == nameof(Length)); } }
        public PeField? String { get { if (!EnsureLoaded()) return null; return _fields.First(item => item.Name == nameof(String)); } }

        #endregion

        public PeRsrcDirectoryString(PeFile root, string filePath, long structureStartOffset, ILogger logger)
            : base(root, filePath, logger)
        {
            _structureStartOffset = structureStartOffset;
        }

        private bool EnsureLoaded()
        {
            if (!_isLoaded)
            {
                if (!LoadStructureDefinition())
                    return false;
                if (!LoadStructureData())
                    return false;
                _isLoaded = true;
            }
            return true;
        }

        private bool LoadStructureDefinition()
        {
            PeRepository repository = new();
            var fieldsResult = repository.GetFields("RsrcDirectoryString");
            if (!fieldsResult.IsSuccess)
            {
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"Failed to retrieve resource directory string fields: {fieldsResult.Error?.Message}");
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

            int lengthIndex = Array.FindIndex(_fields, item => item.Name == nameof(Length));
            int stringIndex = Array.FindIndex(_fields, item => item.Name == nameof(String));
            if (lengthIndex < 0 || stringIndex < 0)
                return false;

            var lengthResult = reader.ReadField(_filePath, _structureStartOffset.Value, _fields[lengthIndex]);
            if (!lengthResult.IsSuccess)
            {
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"Failed to read resource directory string Length: {lengthResult.Error?.Message}");
                return false;
            }
            _fields[lengthIndex] = lengthResult.Value!;

            if (_fields[lengthIndex].Value is null)
                return false;

            int characterCount = Convert.ToUInt16(_fields[lengthIndex].Value);
            _fields[stringIndex].Size = characterCount * 2;

            var stringResult = reader.ReadField(_filePath, _structureStartOffset.Value, _fields[stringIndex]);
            if (!stringResult.IsSuccess)
            {
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"Failed to read resource directory string String: {stringResult.Error?.Message}");
                return false;
            }
            _fields[stringIndex] = stringResult.Value!;
            return true;
        }
    }
}