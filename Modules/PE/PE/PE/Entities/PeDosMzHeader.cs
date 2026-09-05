using Espluque.Contracts.CrossCutting;
using PE.Repositories;

namespace PE.Entities
{
    internal class PeDosMzHeader
    {
        private readonly string _filePath;
        private readonly long _structureStartOffset = 0;

        private readonly string _dbFilePath;
        private readonly ILogger _logger;
        private bool _isLoaded = false;
        private PeField[] _fields = [];

        internal PeField[]? Fields
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields;
            }
        }

        #region Public Properties

        public PeField? EMagic
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(EMagic));
            }
        }

        public PeField? ECblp
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(ECblp));
            }
        }

        public PeField? ECp
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(ECp));
            }
        }

        public PeField? ECrlc
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(ECrlc));
            }
        }

        public PeField? ECparhdr
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(ECparhdr));
            }
        }

        public PeField? EMinalloc
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(EMinalloc));
            }
        }

        public PeField? EMaxalloc
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(EMaxalloc));
            }
        }

        public PeField? ESs
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(ESs));
            }
        }

        public PeField? ESp
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(ESp));
            }
        }

        public PeField? ECsum
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(ECsum));
            }
        }

        public PeField? EIp
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(EIp));
            }
        }

        public PeField? ECs
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(ECs));
            }
        }

        public PeField? ELfarlc
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(ELfarlc));
            }
        }

        public PeField? EOvno
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(EOvno));
            }
        }

        public PeField? ERes
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(ERes));
            }
        }

        public PeField? EOemid
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(EOemid));
            }
        }

        public PeField? EOeminfo
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(EOeminfo));
            }
        }

        public PeField? ERes2
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(ERes2));
            }
        }

        public PeField? ELfanew
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(ELfanew));
            }
        }

        #endregion

        public PeDosMzHeader(string filePath, ILogger logger)
        {
            _filePath = filePath;
            _logger = logger;
            string dllDirectory = Path.GetDirectoryName(typeof(PeDosMzHeader).Assembly.Location)!;
            _dbFilePath = Path.Combine(dllDirectory, "pe.db");
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
            PeRepository repository = new(_dbFilePath);
            var fieldsResult = repository.GetFields("DosMzHeader");

            if (!fieldsResult.IsSuccess)
            {
                _logger.Log(
                    Microsoft.Extensions.Logging.LogLevel.Error,
                    $"Failed to retrieve DOS MZ header fields: {fieldsResult.Error?.Message}");

                return false;
            }

            _fields = fieldsResult.Value!;

            return true;
        }

        private bool LoadStructureData()
        {
            PE.Services.PeReader reader = new();
            string formattedFileName = Path.GetFileName(_filePath).PadRight(35);

            for (int i = 0; i < _fields.Length; i++)
            {
                var result = reader.ReadField(_filePath, _structureStartOffset, _fields[i]);

                if (!result.IsSuccess)
                {
                    _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"{formattedFileName}\tFailed to read DOS MZ header field {_fields[i].Name}: {result.Error?.Message}");
                    return false;
                }

                _fields[i] = result.Value!;
            }

            return true;
        }
    }
}