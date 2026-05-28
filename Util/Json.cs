using System.Text.Json;
using Json.Schema;

namespace Util
{
    public class Json
    {
        public static bool IsValidJson(string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent)) return false;

            try
            {
                using JsonDocument jsonDocument = JsonDocument.Parse(jsonContent);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        public static bool IsJsonValidAgainstJsonSchema(string jsonSchemaContent, string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonSchemaContent)) return false;
            if (string.IsNullOrWhiteSpace(jsonContent)) return false;

            try
            {
                JsonSchema jsonSchema = JsonSchema.FromText(jsonSchemaContent);

                using JsonDocument jsonDocument = JsonDocument.Parse(jsonContent);

                EvaluationResults evaluationResults = jsonSchema.Evaluate(jsonDocument.RootElement);

                return evaluationResults.IsValid;
            }
            catch
            {
                return false;
            }
        }
    }
}
