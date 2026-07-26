using Espluque.Contracts.Entities;
using Espluque.Contracts.Interfaces;
using Espluque.Contracts.MessageInterfaces;
using Espluque.Contracts.ModuleInterfaces.Contributions;
using Espluque.Contracts.Ports;
using Microsoft.Win32;

namespace AnyFile
{
    public class InfosGrabber: IGrabber
    {
        private readonly IMessageCenter _messageCenter;
        private readonly Espluque.Contracts.Ports.ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        public InfosGrabber(IMessageCenter messageCenter,
            Espluque.Contracts.Ports.ILogger logger,
            ISettingsService settingsService,
            IEntityFactory entityFactory)
        {
            _messageCenter = messageCenter;
            _logger = logger;
            _settingsService = settingsService;
            _entityFactory = entityFactory;
        }

        public async Task<List<KeyValuePair<string, string>>> Grab(AnalysisContext analysisContext)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(analysisContext.FilePath);

                List<KeyValuePair<string, string>> infos = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("Name", System.IO.Path.GetFileName(analysisContext.FilePath)),
                    new KeyValuePair<string, string>("Creation Time", fileInfo.CreationTime.ToString()),
                    new KeyValuePair<string, string>("IsReadOnly", fileInfo.IsReadOnly.ToString()),
                    new KeyValuePair<string, string>("LastAccessTime", fileInfo.LastAccessTime.ToString()),
                    new KeyValuePair<string, string>("LastWriteTime", fileInfo.LastWriteTime.ToString()),
                    new KeyValuePair<string, string>("Length", fileInfo.Length.ToString())
                };

                string ext = System.IO.Path.GetExtension(analysisContext.FilePath).ToLowerInvariant();
                string defaultDescription = string.IsNullOrWhiteSpace(ext)
                    ? "file"
                    : $"{ext.Substring(1)} file";

                string typeLabel = string.IsNullOrWhiteSpace(ext) ? "file" : ext;
                string typeDescription = defaultDescription;

                RegistryKey? extTypeKey = Registry.ClassesRoot.OpenSubKey(ext, false);

                if (extTypeKey != null)
                {
                    typeLabel = (string?)extTypeKey.GetValue(string.Empty, defaultDescription) ?? defaultDescription;

                    RegistryKey? extDescriptionKey = Registry.ClassesRoot.OpenSubKey(typeLabel, false);

                    if (extDescriptionKey != null)
                    {
                        typeDescription = (string?)extDescriptionKey.GetValue(string.Empty, defaultDescription) ?? defaultDescription;
                    }
                }

                infos.Add(new KeyValuePair<string, string>("Type Label", typeLabel));
                infos.Add(new KeyValuePair<string, string>("Type Description", typeDescription));

                return new Formatter().Format(infos);
            }
            catch (Exception ex)
            {
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"AnyFile module Infos Reader: {ex.Message}");
                return new List<KeyValuePair<string, string>>();
            }
        }


    }
}
