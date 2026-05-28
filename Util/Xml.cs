using System.Xml;
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
    }
}
