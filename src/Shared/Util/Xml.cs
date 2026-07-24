using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Util
{
    public class Xml
    {
        public static bool IsValidXml(string xmlContent)
        {
            if (string.IsNullOrWhiteSpace(xmlContent)) return false;

            try
            {
                XmlReaderSettings settings = new()
                {
                    DtdProcessing = DtdProcessing.Prohibit
                };

                using StringReader stringReader = new(xmlContent);
                using XmlReader xmlReader = XmlReader.Create(stringReader, settings);

                while (xmlReader.Read())
                {
                }

                return true;
            }
            catch (XmlException)
            {
                return false;
            }
        }

        public static bool IsXmlValidAgainstXsd(string xsdContent, string xmlContent)
        {
            if (string.IsNullOrWhiteSpace(xsdContent)) return false;
            if (string.IsNullOrWhiteSpace(xmlContent)) return false;

            try
            {
                bool isValid = true;

                XmlSchemaSet schemas = new();

                using StringReader xsdStringReader = new(xsdContent);
                using XmlReader xsdReader = XmlReader.Create(xsdStringReader);

                schemas.Add(null, xsdReader);
                schemas.Compile();

                XmlReaderSettings settings = new()
                {
                    ValidationType = ValidationType.Schema,
                    Schemas = schemas,
                    DtdProcessing = DtdProcessing.Prohibit
                };

                settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;

                settings.ValidationEventHandler += (_, _) =>
                {
                    isValid = false;
                };

                using StringReader xmlStringReader = new(xmlContent);
                using XmlReader xmlReader = XmlReader.Create(xmlStringReader, settings);

                while (xmlReader.Read())
                {
                }

                return isValid;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsXmlValidAgainstXsd(List<string> xsdContents, string xmlContent)
        {
            if (xsdContents.Count == 0) return false;
            if (string.IsNullOrWhiteSpace(xmlContent)) return false;

            try
            {
                bool isValid = true;
                XmlSchemaSet schemas = new();

                foreach (string xsdContent in xsdContents)
                {
                    using StringReader xsdStringReader = new(xsdContent);
                    using XmlReader xsdReader = XmlReader.Create(xsdStringReader);

                    schemas.Add(null, xsdReader);
                }

                schemas.Compile();

                XmlReaderSettings settings = new()
                {
                    ValidationType = ValidationType.Schema,
                    Schemas = schemas,
                    DtdProcessing = DtdProcessing.Prohibit
                };

                settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;

                settings.ValidationEventHandler += (_, e) =>
                {
                    string validationMessage = e.Message;
                    int line = e.Exception?.LineNumber ?? 0;
                    int position = e.Exception?.LinePosition ?? 0;
                    XmlSeverityType severity = e.Severity;

                    isValid = false;
                };

                using StringReader xmlStringReader = new(xmlContent);
                using XmlReader xmlReader = XmlReader.Create(xmlStringReader, settings);

                while (xmlReader.Read())
                {
                }

                return isValid;
            }
            catch (Exception ex)
            {
                var exTxt = $"{ex.Message}";
                return false;
            }
        }

        public static async Task<Result<XDocument?>> ReadXDocumentFromFile(string filePath)
        {
            XDocument? xmlDocument;
            Result<bool> canOpenReadResult = Util.File.CanOpenRead(filePath);

            if (!canOpenReadResult.IsSuccess)
            {
                return Result<XDocument?>.Failure(canOpenReadResult.Error!.Code, canOpenReadResult.Error.Message);
            }

            string xmlContent;

            try
            {
                xmlContent = await System.IO.File.ReadAllTextAsync(filePath);
            }
            catch (Exception ex)
            {
                return Result<XDocument?>.Failure(
                    "XML_READ_FAILED",
                    $"Xml.ReadXDocumentFromFile: failed to read file '{filePath}'. {ex.Message}");
            }

            if (string.IsNullOrWhiteSpace(xmlContent))
            {
                return Result<XDocument?>.Failure(
                    "XML_EMPTY",
                    "Xml.ReadXDocumentFromFile: The XML content is empty.");
            }

            try
            {
                xmlDocument = XDocument.Parse(xmlContent);
            }
            catch (Exception exception)
            {
                return Result<XDocument?>.Failure(
                    "XML_INVALID",
                    $"Xml.ReadXDocumentFromFile: invalid XML content. {exception.Message}");
            }

            return Result<XDocument?>.Success(xmlDocument);
        }

    }
}
