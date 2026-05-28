namespace Util
{
    public class File
    {
        public static bool isfilelocked(string filename)
        {
            FileStream stream = null;

            try
            {
                stream = new FileStream(filename, FileMode.Open, FileAccess.Read);
                return false;
            }
            catch
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }
        }

        public static Result<bool> CanOpenRead(string filepath)
        {
            if (string.IsNullOrWhiteSpace(filepath))
            {
                return Result<bool>.Failure(
                    "FILEPATH_EMPTY",
                    "File path is empty.");
            }

            try
            {
                if (System.IO.Directory.Exists(filepath))
                {
                    return Result<bool>.Failure(
                        "TARGET_IS_DIRECTORY",
                        "Target path is a directory.");
                }

                System.IO.FileInfo fileInfo = new System.IO.FileInfo(filepath);

                if (!fileInfo.Exists)
                {
                    return Result<bool>.Failure(
                        "FILE_NOT_FOUND",
                        "File was not found.");
                }

                using System.IO.FileStream fileStream = new System.IO.FileStream(
                    filepath,
                    System.IO.FileMode.Open,
                    System.IO.FileAccess.Read,
                    System.IO.FileShare.Read);

                return Result<bool>.Success(true);
            }
            catch (UnauthorizedAccessException exception)
            {
                return Result<bool>.Failure(
                    "FILE_ACCESS_DENIED",
                    exception.Message);
            }
            catch (IOException exception)
            {
                return Result<bool>.Failure(
                    "FILE_IO_ERROR",
                    exception.Message);
            }
            catch (Exception exception)
            {
                return Result<bool>.Failure(
                    "FILE_OPEN_READ_ERROR",
                    exception.Message);
            }
        }
    }
}
