using Espluque.Contracts.Interfaces;

namespace Espluque.Application.Entities
{
    public class FileInformationPack: IFileInformationPack
    {
        public string Label { get; set; }

        public List<KeyValuePair<string, string>>? Information { get; set; }
    }
}
