using Espluque.Contracts.Enums;
using Espluque.Contracts.Interfaces;

namespace Espluque.Application.Entities
{
    public class Fact : IFact
    {
        /// <summary>
        /// Represents a confirmed information item discovered during analysis.
        /// </summary>
        /// <remarks>
        /// A fact describes one stable result produced by an analyzer, such as a detected
        /// format, an encoding, a structural marker, or a technical attribute.
        /// The source and evidence fields indicate how this fact was established.
        /// </remarks>
        public FactStatusEnum? Status { get; set; }

        public string Key { get; set; } = string.Empty;

        public string? Value { get; set; }

        public string Source { get; set; } = string.Empty;

        public string? Evidence { get; set; }
    }
}

/*
exemples:
| Status        | Key                | Value                         | Source                 | Evidence                                   |
| ------------- | ------------------ | ----------------------------- | ---------------------- | ------------------------------------------ |
| `Detected`    | `file.extension`   | `.msix`                       | `ExtensionAnalyzer`    | `Nom du fichier : app.msix`                |
| `Detected`    | `format.zip`       | `true`                        | `ZipAnalyzer`          | `Octets 0-1 = 50 4B`                       |
| `NotDetected` | `format.json`      | `false`                       | `JsonAnalyzer`         | `Premier caractère incompatible avec JSON` |
| `Failed`      | `archive.zip.read` | `central_directory_not_found` | `ZipAnalyzer`          | `Central directory not found`              |
| `Detected`    | `text.encoding`    | `UTF-8`                       | `TextEncodingAnalyzer` | `Décodage réussi`                          |
| `Failed`      | `text.decode`      | `invalid_utf8_sequence`       | `TextEncodingAnalyzer` | `Octet invalide à l’offset 128`            |

*/