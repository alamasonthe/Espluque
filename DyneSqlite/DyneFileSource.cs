using Espluque.Contracts.Ports;
using System.IO;
using System.Net.Http.Json;
using System.Text.Json;
using Util;

namespace DyneSqlite
{
    public class DyneFileSource : IDyneFileSource
    {
        private readonly IDyneExtensionRepository _extensionRepository;
        private readonly IDyneCategoryRepository _categoryRepository;
        private readonly IDyneCategoryExtensionRepository _categoryExtensionRepository;

        public DyneFileSource(
            IDyneExtensionRepository extensionRepository,
            IDyneCategoryRepository categoryRepository,
            IDyneCategoryExtensionRepository categoryExtensionRepository)
        {
            _extensionRepository = extensionRepository;
            _categoryRepository = categoryRepository;
            _categoryExtensionRepository = categoryExtensionRepository;
        }
        #region CSV

        public async Task<Result<bool>> ImportExtensionFromCsvAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Result<bool>.Failure(
                    "DYNE_EXTENSION_CSV_EMPTY_FILE_PATH",
                    "FileSource.ImportExtensionFromCsvAsync: file path is empty.");
            }

            if (!System.IO.File.Exists(filePath))
            {
                return Result<bool>.Failure(
                    "DYNE_EXTENSION_CSV_FILE_NOT_FOUND",
                    $"FileSource.ImportExtensionFromCsvAsync: file not found '{filePath}'.");
            }

            try
            {
                int lineNumber = 0;

                foreach (string line in System.IO.File.ReadLines(filePath))
                {
                    lineNumber++;

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    string[] columns = line.Split(',');

                    if (columns.Length != 3)
                    {
                        return Result<bool>.Failure(
                            "DYNE_EXTENSION_CSV_INVALID_COLUMN_COUNT",
                            $"FileSource.ImportExtensionFromCsvAsync: invalid column count at line {lineNumber}. Expected 3 columns.");
                    }

                    string extension = columns[0].Trim();
                    string? openClose = NormalizeValue(columns[1]);
                    string? textBinary = NormalizeValue(columns[2]);

                    Result<bool> upsertResult = await _extensionRepository.UpsertAsync(
                        extension,
                        openClose,
                        textBinary);

                    if (!upsertResult.IsSuccess)
                    {
                        return Result<bool>.Failure(
                            upsertResult.Error?.Code ?? "DYNE_EXTENSION_UPSERT_UNKNOWN_ERROR",
                            upsertResult.Error?.Message ?? $"FileSource.ImportExtensionFromCsvAsync: failed to upsert extension at line {lineNumber}.");
                    }

                    if (upsertResult.Value != true)
                    {
                        return Result<bool>.Failure(
                            "DYNE_EXTENSION_UPSERT_NO_ROW_AFFECTED",
                            $"FileSource.ImportExtensionFromCsvAsync: no row affected for extension '{extension}' at line {lineNumber}.");
                    }
                }

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(
                    "DYNE_EXTENSION_CSV_IMPORT_FAILED",
                    $"FileSource.ImportExtensionFromCsvAsync: failed to import file '{filePath}'. {ex.Message}");
            }
        }

        private static string? NormalizeValue(string value)
        {
            string normalizedValue = value.Trim();

            if (string.IsNullOrWhiteSpace(normalizedValue))
            {
                return null;
            }

            return normalizedValue;
        }

        #endregion

        #region JSON

        public async Task<Result<bool>> ImportExtensionFromJsonAsync(string filePath)
        {
            string jsonContent;

            try
            {
                jsonContent = await System.IO.File.ReadAllTextAsync(filePath);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(
                    "DYNE_EXTENSION_JSON_READ_FAILED",
                    $"DyneFileSource.ImportExtensionFromJsonAsync: failed to read file '{filePath}'. {ex.Message}");
            }

            Result<(
                List<string> Extensions,
                List<string> Categories,
                List<KeyValuePair<string, string>> Associations)> parseResult = ParseExtensionJson(jsonContent);

            if (!parseResult.IsSuccess)
            {
                return Result<bool>.Failure(
                    parseResult.Error?.Code ?? "DYNE_EXTENSION_JSON_PARSE_UNKNOWN_ERROR",
                    parseResult.Error?.Message ?? "DyneFileSource.ImportExtensionFromJsonAsync: unknown parse error.");
            }

            foreach (string extension in parseResult.Value.Extensions)
            {
                Result<bool> insertResult = await _extensionRepository.InsertAsync(extension);

                if (!insertResult.IsSuccess)
                {
                    return insertResult;
                }
            }

            foreach (string category in parseResult.Value.Categories)
            {
                Result<bool> insertResult = await _categoryRepository.InsertAsync(category);

                if (!insertResult.IsSuccess)
                {
                    return insertResult;
                }
            }

            foreach (KeyValuePair<string, string> association in parseResult.Value.Associations)
            {
                Result<bool> insertResult = await _categoryExtensionRepository.InsertAsync(
                    association.Key,
                    association.Value);

                if (!insertResult.IsSuccess)
                {
                    return insertResult;
                }
            }

            return Result<bool>.Success(true);
        }

        public Result<(List<string> Extensions, List<string> Categories, List<KeyValuePair<string, string>> Associations)> ParseExtensionJson(string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return Result<(
                    List<string> Extensions,
                    List<string> Categories,
                    List<KeyValuePair<string, string>> Associations)>.Failure(
                        "DYNE_EXTENSION_JSON_EMPTY_CONTENT",
                        "DyneFileSource.ParseExtensionJson: json content is empty.");
            }

            try
            {
                Dictionary<string, List<string>?>? jsonExtensions =
                    JsonSerializer.Deserialize<Dictionary<string, List<string>?>>(jsonContent);

                if (jsonExtensions is null)
                {
                    return Result<(
                        List<string> Extensions,
                        List<string> Categories,
                        List<KeyValuePair<string, string>> Associations)>.Failure(
                            "DYNE_EXTENSION_JSON_DESERIALIZE_NULL",
                            "DyneFileSource.ParseExtensionJson: json deserialization returned null.");
                }

                HashSet<string> extensions = [];
                HashSet<string> categories = [];
                HashSet<(string Extension, string Category)> associationKeys = [];

                List<KeyValuePair<string, string>> associations = [];

                foreach (KeyValuePair<string, List<string>?> jsonExtension in jsonExtensions)
                {
                    string extension = jsonExtension.Key.Trim();

                    if (string.IsNullOrWhiteSpace(extension))
                    {
                        continue;
                    }

                    extensions.Add(extension);

                    if (jsonExtension.Value is null)
                    {
                        continue;
                    }

                    foreach (string categoryValue in jsonExtension.Value)
                    {
                        string category = categoryValue.Trim();

                        if (string.IsNullOrWhiteSpace(category))
                        {
                            continue;
                        }

                        categories.Add(category);

                        if (associationKeys.Add((extension, category)))
                        {
                            associations.Add(new KeyValuePair<string, string>(
                                extension,
                                category));
                        }
                    }
                }

                return Result<(
                    List<string> Extensions,
                    List<string> Categories,
                    List<KeyValuePair<string, string>> Associations)>.Success(
                        (
                            extensions.ToList(),
                            categories.ToList(),
                            associations
                        ));
            }
            catch (Exception ex)
            {
                return Result<(
                    List<string> Extensions,
                    List<string> Categories,
                    List<KeyValuePair<string, string>> Associations)>.Failure(
                        "DYNE_EXTENSION_JSON_PARSE_FAILED",
                        $"DyneFileSource.ParseExtensionJson: failed to parse extensions.json. {ex.Message}");
            }

        }

        #endregion
    }
}
