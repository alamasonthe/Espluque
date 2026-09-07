using PE.Entities;
using Util;

namespace PE.Services
{
    internal class PeReader
    {
        /// <summary>
        /// Reads a single PE field from a file using its relative offset and size.
        /// </summary>
        public Result<PeField> ReadField(string filePath, long structureOffset, PeField field)
        {
            long offset = structureOffset + field.Offset;
            var bytesResult = Bin.ReadBytesFromFile(filePath, offset, field.Size);

            if (!bytesResult.IsSuccess)
                return Result<PeField>.Failure(bytesResult.Error.Code, bytesResult.Error.Message);

            PeField result = new()
            {
                Name = field.Name,
                Offset = field.Offset,
                Size = field.Size,
                Type = field.Type,
                MappingName = field.MappingName,
                DisplayFormat = field.DisplayFormat,
                RawValue = bytesResult.Value
            };

            return Result<PeField>.Success(result);
        }
    }
}