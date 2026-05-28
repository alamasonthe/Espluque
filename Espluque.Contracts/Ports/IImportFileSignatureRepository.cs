using Util;

namespace Espluque.Contracts.Ports
{
    public interface IImportFileSignatureRepository
    {
        Task<Result<bool>> CleanPronomTablesAsync();
        Task<Result<bool>> CloseTransactionAsync(bool commit);
        Task<Result<bool>> CreateTransactionAsync();
        Task<Result<bool>> InsertAnalyzePriorityAsync(int fileFormatId, int hasPriorityOverFileFormatId);
        Task<Result<bool>> InsertByteSequenceAsync(int internalSignatureId, int espluqueSequenceId, string? reference, string? endianness);
        Task<Result<bool>> InsertExtensionAsync(int fileFormatId, string extension);
        Task<Result<bool>> InsertFileFormatAsync(int id, string? mimeType, string name, string puid, string? version);
        Task<Result<bool>> InsertFileFormatInternalSignatureAsync(int fileFormatId, int internalSignatureId);
        Task<Result<bool>> InsertFragmentAsync(int espluqueSequenceId, int subSequencePosition, string leftRight, int maxOffset, int minOffset, int position, string value);
        Task<Result<bool>> InsertInternalSignatureAsync(string id);
        Task<Result<bool>> InsertShiftAsync(int espluqueSequenceId, int subSequencePosition, string byteValue, int value);
        Task<Result<bool>> InsertSubSequenceAsync(int espluqueSequenceId, int minFragLength, int position, int? subSeqMaxOffset, int subSeqMinOffset, string sequence, int defaultShift);
        Task<Result<bool>> UpsertSourceVersionAsync(string sourceName, string? version, string? date);
    }
}